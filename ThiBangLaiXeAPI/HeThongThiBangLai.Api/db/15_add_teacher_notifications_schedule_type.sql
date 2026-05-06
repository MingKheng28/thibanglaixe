/*
    Script: 15_add_teacher_notifications_schedule_type.sql
    Purpose:
      1. Add dedicated teacher profile table.
      2. Backfill teachers from current user roles.
      3. Add teacher/class scope table.
      4. Add notification table.
      5. Add normalized schedule type fields to buoi_hoc.
      6. Add safe class sequence metadata for future class-code generation.

    Safe to re-run: yes, guarded by IF NOT EXISTS checks.
*/

USE [he_thong_thi_bang_lai];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.giao_vien', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.giao_vien
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_giao_vien PRIMARY KEY,
            nguoi_dung_id BIGINT NOT NULL,
            ma_giao_vien VARCHAR(30) NOT NULL,
            ho_ten NVARCHAR(150) NOT NULL,
            ngay_sinh DATE NULL,
            gioi_tinh NVARCHAR(10) NULL,
            cccd VARCHAR(20) NULL,
            so_gplx VARCHAR(50) NULL,
            hang_gplx NVARCHAR(20) NULL,
            chuyen_mon NVARCHAR(255) NULL,
            kinh_nghiem_nam INT NULL,
            trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_giao_vien_trang_thai DEFAULT ('hoat_dong'),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_giao_vien_created_at DEFAULT (SYSUTCDATETIME()),
            updated_at DATETIME2(0) NOT NULL CONSTRAINT df_giao_vien_updated_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT uq_giao_vien_nguoi_dung UNIQUE (nguoi_dung_id),
            CONSTRAINT uq_giao_vien_ma UNIQUE (ma_giao_vien),
            CONSTRAINT uq_giao_vien_cccd UNIQUE (cccd),
            CONSTRAINT fk_giao_vien_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES dbo.nguoi_dung(id)
        );
    END;

    IF OBJECT_ID(N'dbo.giao_vien_lop_hoc', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.giao_vien_lop_hoc
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_giao_vien_lop_hoc PRIMARY KEY,
            giao_vien_id BIGINT NOT NULL,
            lop_hoc_id BIGINT NOT NULL,
            vai_tro_trong_lop VARCHAR(30) NOT NULL CONSTRAINT df_gvlh_vai_tro DEFAULT ('giang_vien'),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_gvlh_created_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT uq_giao_vien_lop_hoc UNIQUE (giao_vien_id, lop_hoc_id),
            CONSTRAINT fk_gvlh_giao_vien FOREIGN KEY (giao_vien_id) REFERENCES dbo.giao_vien(id),
            CONSTRAINT fk_gvlh_lop_hoc FOREIGN KEY (lop_hoc_id) REFERENCES dbo.lop_hoc(id)
        );
    END;

    IF OBJECT_ID(N'dbo.thong_bao', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.thong_bao
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_thong_bao PRIMARY KEY,
            loai VARCHAR(50) NOT NULL,
            muc_do VARCHAR(20) NOT NULL CONSTRAINT df_thong_bao_muc_do DEFAULT ('info'),
            tieu_de NVARCHAR(255) NOT NULL,
            noi_dung NVARCHAR(MAX) NULL,
            doi_tuong VARCHAR(30) NOT NULL,
            nguoi_nhan_id BIGINT NULL,
            entity_type VARCHAR(50) NULL,
            entity_id BIGINT NULL,
            da_doc BIT NOT NULL CONSTRAINT df_thong_bao_da_doc DEFAULT (0),
            trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_thong_bao_trang_thai DEFAULT ('active'),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_thong_bao_created_at DEFAULT (SYSUTCDATETIME()),
            read_at DATETIME2(0) NULL,
            CONSTRAINT fk_thong_bao_nguoi_nhan FOREIGN KEY (nguoi_nhan_id) REFERENCES dbo.nguoi_dung(id)
        );

        CREATE INDEX ix_thong_bao_doi_tuong_da_doc ON dbo.thong_bao(doi_tuong, da_doc, created_at DESC);
        CREATE INDEX ix_thong_bao_nguoi_nhan_da_doc ON dbo.thong_bao(nguoi_nhan_id, da_doc, created_at DESC);
        CREATE INDEX ix_thong_bao_entity ON dbo.thong_bao(entity_type, entity_id);
    END;

    IF COL_LENGTH(N'dbo.buoi_hoc', N'loai_buoi') IS NULL
    BEGIN
        ALTER TABLE dbo.buoi_hoc ADD loai_buoi VARCHAR(30) NOT NULL CONSTRAINT df_buoi_hoc_loai_buoi DEFAULT ('ly_thuyet');
    END;

    IF COL_LENGTH(N'dbo.buoi_hoc', N'dia_diem') IS NULL
    BEGIN
        ALTER TABLE dbo.buoi_hoc ADD dia_diem NVARCHAR(255) NULL;
    END;

    IF COL_LENGTH(N'dbo.buoi_hoc', N'giao_vien_id') IS NULL
    BEGIN
        ALTER TABLE dbo.buoi_hoc ADD giao_vien_id BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.buoi_hoc', N'ghi_chu') IS NULL
    BEGIN
        ALTER TABLE dbo.buoi_hoc ADD ghi_chu NVARCHAR(500) NULL;
    END;

    IF COL_LENGTH(N'dbo.lop_hoc', N'so_thu_tu') IS NULL
    BEGIN
        ALTER TABLE dbo.lop_hoc ADD so_thu_tu INT NULL;
    END;

    IF COL_LENGTH(N'dbo.lop_hoc', N'giao_vien_ho_so_id') IS NULL
    BEGIN
        ALTER TABLE dbo.lop_hoc ADD giao_vien_ho_so_id BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.diem_danh', N'giao_vien_ho_so_id') IS NULL
    BEGIN
        ALTER TABLE dbo.diem_danh ADD giao_vien_ho_so_id BIGINT NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_buoi_hoc_giao_vien_ho_so')
    BEGIN
        ALTER TABLE dbo.buoi_hoc WITH CHECK ADD CONSTRAINT fk_buoi_hoc_giao_vien_ho_so FOREIGN KEY (giao_vien_id) REFERENCES dbo.giao_vien(id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_lop_hoc_giao_vien_ho_so')
    BEGIN
        ALTER TABLE dbo.lop_hoc WITH CHECK ADD CONSTRAINT fk_lop_hoc_giao_vien_ho_so FOREIGN KEY (giao_vien_ho_so_id) REFERENCES dbo.giao_vien(id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_diem_danh_giao_vien_ho_so')
    BEGIN
        ALTER TABLE dbo.diem_danh WITH CHECK ADD CONSTRAINT fk_diem_danh_giao_vien_ho_so FOREIGN KEY (giao_vien_ho_so_id) REFERENCES dbo.giao_vien(id);
    END;

    ;WITH TeacherUsers AS
    (
        SELECT DISTINCT
            nd.id AS nguoi_dung_id,
            nd.ten_dang_nhap,
            nd.email,
            nd.so_dien_thoai,
            ROW_NUMBER() OVER (ORDER BY nd.id) AS rn
        FROM dbo.nguoi_dung AS nd
        INNER JOIN dbo.nguoi_dung_vai_tro AS ndvt ON ndvt.nguoi_dung_id = nd.id
        INNER JOIN dbo.vai_tro AS vt ON vt.id = ndvt.vai_tro_id
        WHERE vt.ma_vai_tro IN ('giao_vien', 'teacher', 'TEACHER')
    )
    INSERT INTO dbo.giao_vien
    (
        nguoi_dung_id,
        ma_giao_vien,
        ho_ten,
        so_gplx,
        chuyen_mon,
        trang_thai
    )
    SELECT
        tu.nguoi_dung_id,
        CONCAT('GV', RIGHT(CONCAT('000000', tu.rn), 6)),
        COALESCE(NULLIF(LTRIM(RTRIM(tu.ten_dang_nhap)), ''), CONCAT(N'Giáo viên ', tu.nguoi_dung_id)),
        NULL,
        N'Đào tạo lái xe',
        'hoat_dong'
    FROM TeacherUsers AS tu
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.giao_vien AS gv
        WHERE gv.nguoi_dung_id = tu.nguoi_dung_id
    );

    UPDATE lh
    SET giao_vien_ho_so_id = gv.id
    FROM dbo.lop_hoc AS lh
    INNER JOIN dbo.giao_vien AS gv ON gv.nguoi_dung_id = lh.giao_vien_id
    WHERE lh.giao_vien_id IS NOT NULL
      AND lh.giao_vien_ho_so_id IS NULL;

    INSERT INTO dbo.giao_vien_lop_hoc (giao_vien_id, lop_hoc_id, vai_tro_trong_lop)
    SELECT DISTINCT
        lh.giao_vien_ho_so_id,
        lh.id,
        'giang_vien_chinh'
    FROM dbo.lop_hoc AS lh
    WHERE lh.giao_vien_ho_so_id IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.giao_vien_lop_hoc AS gvlh
          WHERE gvlh.giao_vien_id = lh.giao_vien_ho_so_id
            AND gvlh.lop_hoc_id = lh.id
      );

    ;WITH ClassSeq AS
    (
        SELECT
            id,
            ROW_NUMBER() OVER (PARTITION BY khoa_hoc_id ORDER BY ngay_bat_dau, id) AS rn
        FROM dbo.lop_hoc
        WHERE so_thu_tu IS NULL
    )
    UPDATE lh
    SET so_thu_tu = cs.rn
    FROM dbo.lop_hoc AS lh
    INNER JOIN ClassSeq AS cs ON cs.id = lh.id;

    UPDATE dbo.buoi_hoc
    SET
        loai_buoi = CASE
            WHEN LOWER(CONCAT(ISNULL(noi_dung, N''), N' ', ISNULL(phong_hoc, N''), N' ', ISNULL(ten_buoi, N''))) LIKE N'%thực hành%' THEN 'thuc_hanh'
            WHEN LOWER(CONCAT(ISNULL(noi_dung, N''), N' ', ISNULL(phong_hoc, N''), N' ', ISNULL(ten_buoi, N''))) LIKE N'%thi thử%' THEN 'thi_thu'
            WHEN LOWER(CONCAT(ISNULL(noi_dung, N''), N' ', ISNULL(phong_hoc, N''), N' ', ISNULL(ten_buoi, N''))) LIKE N'%ôn tập%' THEN 'on_tap'
            ELSE 'ly_thuyet'
        END,
        dia_diem = COALESCE(dia_diem, phong_hoc)
    WHERE loai_buoi = 'ly_thuyet'
       OR dia_diem IS NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_buoi_hoc_loai_ngay' AND object_id = OBJECT_ID(N'dbo.buoi_hoc'))
    BEGIN
        CREATE INDEX ix_buoi_hoc_loai_ngay ON dbo.buoi_hoc(loai_buoi, ngay_hoc);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_lop_hoc_giao_vien_ho_so' AND object_id = OBJECT_ID(N'dbo.lop_hoc'))
    BEGIN
        CREATE INDEX ix_lop_hoc_giao_vien_ho_so ON dbo.lop_hoc(giao_vien_ho_so_id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'uq_lop_hoc_khoa_hoc_so_thu_tu' AND object_id = OBJECT_ID(N'dbo.lop_hoc'))
    BEGIN
        CREATE UNIQUE INDEX uq_lop_hoc_khoa_hoc_so_thu_tu ON dbo.lop_hoc(khoa_hoc_id, so_thu_tu) WHERE so_thu_tu IS NOT NULL;
    END;

    INSERT INTO dbo.thong_bao (loai, muc_do, tieu_de, noi_dung, doi_tuong, entity_type, entity_id)
    SELECT TOP (1)
        'system_upgrade',
        'info',
        N'Đã nâng cấp dữ liệu giáo viên và lịch học',
        N'Hệ thống đã bổ sung hồ sơ giáo viên, phân loại lịch học lý thuyết/thực hành và bảng thông báo.',
        'admin',
        'database_script',
        15
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.thong_bao WHERE loai = 'system_upgrade' AND entity_type = 'database_script' AND entity_id = 15
    );

    COMMIT TRANSACTION;

    SELECT N'15_add_teacher_notifications_schedule_type.sql completed' AS message;
    SELECT COUNT(1) AS total_teachers FROM dbo.giao_vien;
    SELECT loai_buoi, COUNT(1) AS total_schedules FROM dbo.buoi_hoc GROUP BY loai_buoi ORDER BY loai_buoi;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
