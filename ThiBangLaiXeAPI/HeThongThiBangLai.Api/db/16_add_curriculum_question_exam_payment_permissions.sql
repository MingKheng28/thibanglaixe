/*
    Script: 16_add_curriculum_question_exam_payment_permissions.sql
    Purpose:
      1. Add curriculum and lesson tables.
      2. Add question metadata for admin-grade question bank.
      3. Add exam-paper/session management metadata.
      4. Link receipts to course registrations for payment-gated class approval.
      5. Seed teacher/admin permissions for the expanded flow.
      6. Backfill exam_results from finished bai_thi rows.

    Safe to re-run: yes, guarded by IF NOT EXISTS checks.
*/

USE [he_thong_thi_bang_lai];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.giao_trinh', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.giao_trinh
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_giao_trinh PRIMARY KEY,
            ma_giao_trinh VARCHAR(30) NOT NULL,
            ten_giao_trinh NVARCHAR(150) NOT NULL,
            hang_bang NVARCHAR(20) NULL,
            mo_ta NVARCHAR(500) NULL,
            trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_giao_trinh_trang_thai DEFAULT ('active'),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_giao_trinh_created_at DEFAULT (SYSUTCDATETIME()),
            updated_at DATETIME2(0) NOT NULL CONSTRAINT df_giao_trinh_updated_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT uq_giao_trinh_ma UNIQUE (ma_giao_trinh)
        );
    END;

    IF OBJECT_ID(N'dbo.bai_hoc', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.bai_hoc
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_bai_hoc PRIMARY KEY,
            giao_trinh_id BIGINT NOT NULL,
            ma_bai_hoc VARCHAR(30) NOT NULL,
            ten_bai_hoc NVARCHAR(150) NOT NULL,
            loai_bai_hoc VARCHAR(30) NOT NULL,
            thu_tu INT NOT NULL,
            noi_dung NVARCHAR(MAX) NULL,
            thoi_luong_phut INT NULL,
            trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_bai_hoc_trang_thai DEFAULT ('active'),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_bai_hoc_created_at DEFAULT (SYSUTCDATETIME()),
            updated_at DATETIME2(0) NOT NULL CONSTRAINT df_bai_hoc_updated_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT fk_bai_hoc_giao_trinh FOREIGN KEY (giao_trinh_id) REFERENCES dbo.giao_trinh(id),
            CONSTRAINT uq_bai_hoc_giao_trinh_ma UNIQUE (giao_trinh_id, ma_bai_hoc),
            CONSTRAINT uq_bai_hoc_giao_trinh_thu_tu UNIQUE (giao_trinh_id, thu_tu)
        );
    END;

    IF COL_LENGTH(N'dbo.chu_de_cau_hoi', N'bai_hoc_id') IS NULL
    BEGIN
        ALTER TABLE dbo.chu_de_cau_hoi ADD bai_hoc_id BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.chu_de_cau_hoi', N'trang_thai') IS NULL
    BEGIN
        ALTER TABLE dbo.chu_de_cau_hoi ADD trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_chu_de_cau_hoi_trang_thai DEFAULT ('active');
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_chu_de_cau_hoi_bai_hoc')
    BEGIN
        ALTER TABLE dbo.chu_de_cau_hoi WITH CHECK ADD CONSTRAINT fk_chu_de_cau_hoi_bai_hoc FOREIGN KEY (bai_hoc_id) REFERENCES dbo.bai_hoc(id);
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'ma_cau_hoi') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD ma_cau_hoi VARCHAR(30) NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'hang_bang') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD hang_bang NVARCHAR(20) NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'created_by') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD created_by BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'updated_by') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD updated_by BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'approved_by') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD approved_by BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'approved_at') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD approved_at DATETIME2(0) NULL;
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'created_at') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD created_at DATETIME2(0) NOT NULL CONSTRAINT df_cau_hoi_created_at DEFAULT (SYSUTCDATETIME());
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'updated_at') IS NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi ADD updated_at DATETIME2(0) NOT NULL CONSTRAINT df_cau_hoi_updated_at DEFAULT (SYSUTCDATETIME());
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'ma_cau_hoi') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            ;WITH NumberedQuestions AS
            (
                SELECT id, ROW_NUMBER() OVER (ORDER BY id) AS rn
                FROM dbo.cau_hoi
                WHERE ma_cau_hoi IS NULL OR LTRIM(RTRIM(ma_cau_hoi)) = ''''
            )
            UPDATE ch
            SET ma_cau_hoi = CONCAT(''CH'', RIGHT(CONCAT(''000000'', nq.rn), 6))
            FROM dbo.cau_hoi AS ch
            INNER JOIN NumberedQuestions AS nq ON nq.id = ch.id;
        ';
    END;

    IF COL_LENGTH(N'dbo.cau_hoi', N'ma_cau_hoi') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'uq_cau_hoi_ma' AND object_id = OBJECT_ID(N'dbo.cau_hoi'))
    BEGIN
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX uq_cau_hoi_ma ON dbo.cau_hoi(ma_cau_hoi) WHERE ma_cau_hoi IS NOT NULL;';
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_cau_hoi_created_by') AND COL_LENGTH(N'dbo.cau_hoi', N'created_by') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi WITH CHECK ADD CONSTRAINT fk_cau_hoi_created_by FOREIGN KEY (created_by) REFERENCES dbo.nguoi_dung(id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_cau_hoi_updated_by') AND COL_LENGTH(N'dbo.cau_hoi', N'updated_by') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi WITH CHECK ADD CONSTRAINT fk_cau_hoi_updated_by FOREIGN KEY (updated_by) REFERENCES dbo.nguoi_dung(id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_cau_hoi_approved_by') AND COL_LENGTH(N'dbo.cau_hoi', N'approved_by') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.cau_hoi WITH CHECK ADD CONSTRAINT fk_cau_hoi_approved_by FOREIGN KEY (approved_by) REFERENCES dbo.nguoi_dung(id);
    END;

    IF COL_LENGTH(N'dbo.de_thi', N'published_at') IS NULL
    BEGIN
        ALTER TABLE dbo.de_thi ADD published_at DATETIME2(0) NULL;
    END;

    IF COL_LENGTH(N'dbo.de_thi', N'published_by') IS NULL
    BEGIN
        ALTER TABLE dbo.de_thi ADD published_by BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.de_thi', N'hang_bang') IS NULL
    BEGIN
        ALTER TABLE dbo.de_thi ADD hang_bang NVARCHAR(20) NULL;
    END;

    IF COL_LENGTH(N'dbo.de_thi', N'is_public') IS NULL
    BEGIN
        ALTER TABLE dbo.de_thi ADD is_public BIT NOT NULL CONSTRAINT df_de_thi_is_public DEFAULT (0);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_de_thi_published_by')
    BEGIN
        ALTER TABLE dbo.de_thi WITH CHECK ADD CONSTRAINT fk_de_thi_published_by FOREIGN KEY (published_by) REFERENCES dbo.nguoi_dung(id);
    END;

    IF COL_LENGTH(N'dbo.de_thi', N'is_public') IS NOT NULL
       AND COL_LENGTH(N'dbo.de_thi', N'published_at') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            UPDATE dbo.de_thi
            SET loai_de_thi = CASE
                    WHEN loai_de_thi IS NULL OR LTRIM(RTRIM(loai_de_thi)) = '''' THEN ''thi_thu''
                    WHEN loai_de_thi IN (''mock'', ''sample'', ''de_thi_thu'') THEN ''thi_thu''
                    WHEN loai_de_thi IN (''real'', ''official'', ''sat_hach_that'') THEN ''sat_hach''
                    ELSE loai_de_thi
                END,
                is_public = CASE WHEN trang_thai IN (''hoat_dong'', ''published'', ''cong_bo'') THEN 1 ELSE is_public END,
                published_at = CASE WHEN trang_thai IN (''hoat_dong'', ''published'', ''cong_bo'') AND published_at IS NULL THEN SYSUTCDATETIME() ELSE published_at END;
        ';
    END;

    IF OBJECT_ID(N'dbo.ca_thi_de_thi', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ca_thi_de_thi
        (
            id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_ca_thi_de_thi PRIMARY KEY,
            ca_thi_id BIGINT NOT NULL,
            de_thi_id BIGINT NOT NULL,
            is_primary BIT NOT NULL CONSTRAINT df_ctdt_is_primary DEFAULT (0),
            created_at DATETIME2(0) NOT NULL CONSTRAINT df_ctdt_created_at DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT uq_ca_thi_de_thi UNIQUE (ca_thi_id, de_thi_id),
            CONSTRAINT fk_ctdt_ca_thi FOREIGN KEY (ca_thi_id) REFERENCES dbo.ca_thi(id),
            CONSTRAINT fk_ctdt_de_thi FOREIGN KEY (de_thi_id) REFERENCES dbo.de_thi(id)
        );
    END;

    IF COL_LENGTH(N'dbo.ca_thi', N'giam_thi_id') IS NULL
    BEGIN
        ALTER TABLE dbo.ca_thi ADD giam_thi_id BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.ca_thi', N'trang_thai') IS NULL
    BEGIN
        ALTER TABLE dbo.ca_thi ADD trang_thai VARCHAR(30) NOT NULL CONSTRAINT df_ca_thi_trang_thai DEFAULT ('sap_dien_ra');
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_ca_thi_giam_thi')
    BEGIN
        ALTER TABLE dbo.ca_thi WITH CHECK ADD CONSTRAINT fk_ca_thi_giam_thi FOREIGN KEY (giam_thi_id) REFERENCES dbo.giao_vien(id);
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'dang_ky_khoa_hoc_id') IS NULL
    BEGIN
        ALTER TABLE dbo.phieu_thu ADD dang_ky_khoa_hoc_id BIGINT NULL;
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'phuong_thuc_thanh_toan') IS NULL
    BEGIN
        ALTER TABLE dbo.phieu_thu ADD phuong_thuc_thanh_toan VARCHAR(30) NULL;
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'ngay_xac_nhan') IS NULL
    BEGIN
        ALTER TABLE dbo.phieu_thu ADD ngay_xac_nhan DATETIME2(0) NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_phieu_thu_dang_ky_khoa_hoc')
    BEGIN
        ALTER TABLE dbo.phieu_thu WITH CHECK ADD CONSTRAINT fk_phieu_thu_dang_ky_khoa_hoc FOREIGN KEY (dang_ky_khoa_hoc_id) REFERENCES dbo.dang_ky_khoa_hoc(id);
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'dang_ky_khoa_hoc_id') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            UPDATE pt
            SET dang_ky_khoa_hoc_id = matched.registration_id
            FROM dbo.phieu_thu AS pt
            OUTER APPLY
            (
                SELECT TOP (1) dkkh.id AS registration_id
                FROM dbo.dang_ky_khoa_hoc AS dkkh
                WHERE dkkh.hoc_vien_id = pt.hoc_vien_id
                ORDER BY dkkh.ngay_dang_ky DESC, dkkh.id DESC
            ) AS matched
            WHERE pt.dang_ky_khoa_hoc_id IS NULL
              AND matched.registration_id IS NOT NULL;
        ';
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'ngay_xac_nhan') IS NOT NULL
    BEGIN
        EXEC sys.sp_executesql N'
            UPDATE dbo.phieu_thu
            SET ngay_xac_nhan = ISNULL(ngay_xac_nhan, ngay_thu)
            WHERE trang_thai = ''da_xac_nhan''
              AND ngay_xac_nhan IS NULL;
        ';
    END;

    IF COL_LENGTH(N'dbo.phieu_thu', N'dang_ky_khoa_hoc_id') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_phieu_thu_dang_ky_khoa_hoc' AND object_id = OBJECT_ID(N'dbo.phieu_thu'))
    BEGIN
        EXEC sys.sp_executesql N'CREATE INDEX ix_phieu_thu_dang_ky_khoa_hoc ON dbo.phieu_thu(dang_ky_khoa_hoc_id, trang_thai);';
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.giao_trinh WHERE ma_giao_trinh = 'GT-A1-CORE')
    BEGIN
        INSERT INTO dbo.giao_trinh (ma_giao_trinh, ten_giao_trinh, hang_bang, mo_ta, trang_thai)
        VALUES ('GT-A1-CORE', N'Giáo trình đào tạo GPLX hạng A1', N'A1', N'Khung chương trình lý thuyết và thực hành cơ bản cho học viên A1.', 'active');
    END;

    DECLARE @CurriculumA1 BIGINT = (SELECT TOP (1) id FROM dbo.giao_trinh WHERE ma_giao_trinh = 'GT-A1-CORE');

    IF @CurriculumA1 IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.bai_hoc WHERE giao_trinh_id = @CurriculumA1 AND ma_bai_hoc = 'A1-LT-01')
            INSERT INTO dbo.bai_hoc (giao_trinh_id, ma_bai_hoc, ten_bai_hoc, loai_bai_hoc, thu_tu, noi_dung, thoi_luong_phut)
            VALUES (@CurriculumA1, 'A1-LT-01', N'Luật giao thông đường bộ', 'ly_thuyet', 1, N'Quy tắc giao thông, biển báo, sa hình và văn hóa giao thông.', 120);

        IF NOT EXISTS (SELECT 1 FROM dbo.bai_hoc WHERE giao_trinh_id = @CurriculumA1 AND ma_bai_hoc = 'A1-TH-01')
            INSERT INTO dbo.bai_hoc (giao_trinh_id, ma_bai_hoc, ten_bai_hoc, loai_bai_hoc, thu_tu, noi_dung, thoi_luong_phut)
            VALUES (@CurriculumA1, 'A1-TH-01', N'Thực hành kỹ năng điều khiển xe', 'thuc_hanh', 2, N'Bài số 8, đường thẳng, đường vòng và bài thi tổng hợp.', 180);
    END;

    DECLARE @TeacherPermissions TABLE (ma_quyen VARCHAR(50) NOT NULL PRIMARY KEY, ten_quyen NVARCHAR(100) NOT NULL, mo_ta NVARCHAR(255) NULL);
    INSERT INTO @TeacherPermissions (ma_quyen, ten_quyen, mo_ta)
    VALUES
    ('courses.read', N'Xem khóa học', N'Cho phép xem danh sách và chi tiết khóa học'),
    ('courses.create', N'Tạo khóa học', N'Cho phép tạo khóa học'),
    ('courses.update', N'Cập nhật khóa học', N'Cho phép cập nhật khóa học'),
    ('courses.delete', N'Xóa khóa học', N'Cho phép xóa hoặc ngưng khóa học'),
    ('classes.read', N'Xem lớp học', N'Cho phép xem lớp học'),
    ('classes.create', N'Tạo lớp học', N'Cho phép tạo lớp học'),
    ('classes.update', N'Cập nhật lớp học', N'Cho phép cập nhật lớp học'),
    ('classes.delete', N'Xóa lớp học', N'Cho phép xóa hoặc đóng lớp học'),
    ('schedules.read', N'Xem lịch học', N'Cho phép xem lịch học'),
    ('schedules.create', N'Tạo lịch học', N'Cho phép xếp lịch học'),
    ('schedules.update', N'Cập nhật lịch học', N'Cho phép cập nhật lịch học'),
    ('schedules.delete', N'Xóa lịch học', N'Cho phép xóa lịch học'),
    ('questions.read', N'Xem câu hỏi', N'Cho phép xem ngân hàng câu hỏi'),
    ('questions.create', N'Tạo câu hỏi', N'Cho phép tạo câu hỏi'),
    ('questions.update', N'Cập nhật câu hỏi', N'Cho phép cập nhật câu hỏi'),
    ('questions.delete', N'Xóa câu hỏi', N'Cho phép xóa câu hỏi'),
    ('questions.approve', N'Duyệt câu hỏi', N'Cho phép duyệt câu hỏi'),
    ('exam_papers.read', N'Xem đề thi', N'Cho phép xem đề thi'),
    ('exam_papers.create', N'Tạo đề thi', N'Cho phép tạo đề thi'),
    ('exam_papers.update', N'Cập nhật đề thi', N'Cho phép cập nhật đề thi'),
    ('exam_papers.delete', N'Xóa đề thi', N'Cho phép xóa đề thi'),
    ('exam_papers.publish', N'Công bố đề thi', N'Cho phép công bố đề thi'),
    ('exam_sessions.read', N'Xem kỳ thi/ca thi', N'Cho phép xem kỳ thi và ca thi'),
    ('exam_sessions.create', N'Tạo kỳ thi/ca thi', N'Cho phép tạo kỳ thi và ca thi'),
    ('exam_sessions.update', N'Cập nhật kỳ thi/ca thi', N'Cho phép cập nhật kỳ thi và ca thi'),
    ('exam_sessions.delete', N'Xóa kỳ thi/ca thi', N'Cho phép xóa kỳ thi và ca thi'),
    ('students.read_by_class', N'Xem học viên theo lớp', N'Cho phép xem học viên thuộc lớp được phân công'),
    ('receipts.read', N'Xem phiếu thu', N'Cho phép xem phiếu thu'),
    ('receipts.create', N'Tạo phiếu thu', N'Cho phép tạo phiếu thu'),
    ('receipts.confirm', N'Xác nhận phiếu thu', N'Cho phép xác nhận phiếu thu'),
    ('receipts.cancel', N'Hủy phiếu thu', N'Cho phép hủy phiếu thu'),
    ('enrollments.approve_to_class', N'Duyệt học viên vào lớp', N'Cho phép duyệt học viên vào lớp sau khi đủ học phí');

    INSERT INTO dbo.quyen_han (ma_quyen, ten_quyen, mo_ta)
    SELECT p.ma_quyen, p.ten_quyen, p.mo_ta
    FROM @TeacherPermissions AS p
    WHERE NOT EXISTS (SELECT 1 FROM dbo.quyen_han AS q WHERE q.ma_quyen = p.ma_quyen);

    DECLARE @TeacherRoleId BIGINT = (SELECT TOP (1) id FROM dbo.vai_tro WHERE ma_vai_tro IN ('giao_vien', 'teacher', 'TEACHER') ORDER BY CASE WHEN ma_vai_tro = 'giao_vien' THEN 0 ELSE 1 END);

    IF @TeacherRoleId IS NOT NULL
    BEGIN
        INSERT INTO dbo.vai_tro_quyen_han (vai_tro_id, quyen_han_id)
        SELECT @TeacherRoleId, q.id
        FROM dbo.quyen_han AS q
        INNER JOIN @TeacherPermissions AS p ON p.ma_quyen = q.ma_quyen
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.vai_tro_quyen_han AS vtqh
            WHERE vtqh.vai_tro_id = @TeacherRoleId
              AND vtqh.quyen_han_id = q.id
        );
    END;

    INSERT INTO dbo.exam_results
    (
        bai_thi_id,
        hoc_vien_id,
        tong_so_cau,
        so_cau_dung,
        diem,
        ket_qua,
        xac_nhan_luc,
        created_at,
        updated_at
    )
    SELECT
        bt.id,
        bt.hoc_vien_id,
        bt.tong_so_cau,
        bt.so_cau_dung,
        bt.diem,
        bt.ket_qua,
        CASE WHEN bt.trang_thai IN ('da_nop', 'hoan_thanh') THEN SYSUTCDATETIME() ELSE NULL END,
        SYSUTCDATETIME(),
        SYSUTCDATETIME()
    FROM dbo.bai_thi AS bt
    WHERE bt.hoc_vien_id IS NOT NULL
      AND bt.trang_thai IN ('da_nop', 'hoan_thanh')
      AND NOT EXISTS (SELECT 1 FROM dbo.exam_results AS er WHERE er.bai_thi_id = bt.id);

    IF OBJECT_ID(N'dbo.thong_bao', N'U') IS NOT NULL
    BEGIN
        INSERT INTO dbo.thong_bao (loai, muc_do, tieu_de, noi_dung, doi_tuong, entity_type, entity_id)
        SELECT TOP (1)
            'system_upgrade',
            'info',
            N'Đã nâng cấp giáo trình, đề thi, học phí và phân quyền',
            N'Hệ thống đã bổ sung giáo trình/bài học, metadata câu hỏi, mapping ca thi-đề thi, liên kết phiếu thu với đăng ký khóa học và quyền giáo viên.',
            'admin',
            'database_script',
            16
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.thong_bao WHERE loai = 'system_upgrade' AND entity_type = 'database_script' AND entity_id = 16
        );
    END;

    COMMIT TRANSACTION;

    SELECT N'16_add_curriculum_question_exam_payment_permissions.sql completed' AS message;
    SELECT COUNT(1) AS total_curriculums FROM dbo.giao_trinh;
    SELECT COUNT(1) AS total_lessons FROM dbo.bai_hoc;
    SELECT COUNT(1) AS total_permissions FROM dbo.quyen_han;
    SELECT COUNT(1) AS total_exam_results FROM dbo.exam_results;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
