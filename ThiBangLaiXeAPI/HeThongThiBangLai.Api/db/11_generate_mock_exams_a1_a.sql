SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @LoaiDeThiThiThu NVARCHAR(50) = N'thi_thu';
    DECLARE @MaKyThi VARCHAR(30) = 'KYTHI_MO_PHONG_A1A';
    -- File nay can duoc luu/chay bang UTF-8 de giu dung tieng Viet co dau.
    -- Neu chay bang sqlcmd, dung them tham so: -f 65001
    DECLARE @TenKyThi NVARCHAR(150) = N'Kỳ thi mô phỏng A1/A';
    DECLARE @NgayThi DATE = CAST(GETDATE() AS DATE);
    DECLARE @NguoiTaoId BIGINT = (SELECT TOP 1 id FROM nguoi_dung ORDER BY id);
    DECLARE @KyThiId BIGINT;

    -- Bảo đảm cột loai_de_thi đã tồn tại.
    IF COL_LENGTH('de_thi', 'loai_de_thi') IS NULL
    BEGIN
        EXEC(N'ALTER TABLE de_thi ADD loai_de_thi NVARCHAR(50) NULL;');
    END;

    -- Tạo hoặc lấy kỳ thi dùng chung cho các đề mô phỏng A1/A.
    SELECT @KyThiId = id
    FROM ky_thi
    WHERE ma_ky_thi = @MaKyThi;

    IF @KyThiId IS NULL
    BEGIN
        INSERT INTO ky_thi (ma_ky_thi, ten_ky_thi, ngay_thi, mo_ta, trang_thai)
        VALUES
        (
            @MaKyThi,
            @TenKyThi,
            @NgayThi,
            N'Kỳ thi dùng để gom 10 đề thi thử A1/A 25 câu theo đúng cơ cấu đề thi.',
            'hoat_dong'
        );

        SET @KyThiId = SCOPE_IDENTITY();
    END;

    DECLARE @MockExamIds TABLE (id BIGINT PRIMARY KEY);

    INSERT INTO @MockExamIds (id)
    SELECT id
    FROM de_thi
    WHERE loai_de_thi = @LoaiDeThiThiThu;

    -- Xóa dữ liệu thi thử cũ trước khi tạo mới.
    DELETE ctbt
    FROM chi_tiet_bai_thi AS ctbt
    INNER JOIN bai_thi AS bt ON bt.id = ctbt.bai_thi_id
    INNER JOIN @MockExamIds AS me ON me.id = bt.de_thi_id;

    DELETE bt
    FROM bai_thi AS bt
    INNER JOIN @MockExamIds AS me ON me.id = bt.de_thi_id;

    DELETE dtch
    FROM de_thi_cau_hoi AS dtch
    INNER JOIN @MockExamIds AS me ON me.id = dtch.de_thi_id;

    DELETE dt
    FROM de_thi AS dt
    INNER JOIN @MockExamIds AS me ON me.id = dt.id;

    -- Reset identity ve gia tri lon nhat con lai sau khi xoa de thi thu cu.
    -- Neu bang khong con dong nao thi reseed ve 0, dong insert tiep theo se co id = 1.
    DECLARE @MaxDeThiId BIGINT = ISNULL((SELECT MAX(id) FROM de_thi), 0);
    DECLARE @MaxDeThiCauHoiId BIGINT = ISNULL((SELECT MAX(id) FROM de_thi_cau_hoi), 0);

    DBCC CHECKIDENT ('de_thi', RESEED, @MaxDeThiId) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('de_thi_cau_hoi', RESEED, @MaxDeThiCauHoiId) WITH NO_INFOMSGS;

    -- Kiểm tra dữ liệu đầu vào tối thiểu để giữ đúng cơ cấu 25 câu / đề.
    -- 10 đề cần tối thiểu:
    -- 80 câu chủ đề 1, 10 câu điểm liệt chủ đề 2, 10 câu chủ đề 3,
    -- 10 câu chủ đề 4, 80 câu chủ đề 5, 60 câu chủ đề 6.
    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 1 AND trang_thai = 'approved') < 80
        THROW 51001, N'Không đủ 80 câu chủ đề 1 để tạo 10 đề thi thử A1/A.', 1;

    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 2 AND la_cau_diem_liet = 1 AND trang_thai = 'approved') < 10
        THROW 51002, N'Không đủ 10 câu điểm liệt chủ đề 2 để tạo 10 đề thi thử A1/A.', 1;

    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 3 AND trang_thai = 'approved') < 10
        THROW 51003, N'Không đủ 10 câu chủ đề 3 để tạo 10 đề thi thử A1/A.', 1;

    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 4 AND trang_thai = 'approved') < 10
        THROW 51004, N'Không đủ 10 câu chủ đề 4 để tạo 10 đề thi thử A1/A.', 1;

    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 5 AND trang_thai = 'approved') < 80
        THROW 51005, N'Không đủ 80 câu chủ đề 5 để tạo 10 đề thi thử A1/A.', 1;

    IF (SELECT COUNT(*) FROM cau_hoi WHERE chu_de_id = 6 AND trang_thai = 'approved') < 1
        THROW 51006, N'Không có câu sa hình chủ đề 6 để tạo đề thi thử A1/A.', 1;

    DECLARE @QuestionPool TABLE
    (
        nhom_cau_hoi INT NOT NULL,
        rn INT NOT NULL,
        cau_hoi_id BIGINT NOT NULL,
        PRIMARY KEY (nhom_cau_hoi, rn)
    );

    -- Nhóm 1: 08 câu / đề - Một số quy định chung và quy tắc giao thông đường bộ.
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 1, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 1 AND trang_thai = 'approved';

    -- Nhóm 2: 01 câu / đề - Tình huống mất an toàn giao thông nghiêm trọng (câu điểm liệt).
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 2, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 2 AND la_cau_diem_liet = 1 AND trang_thai = 'approved';

    -- Nhóm 3: 01 câu / đề - Văn hóa giao thông, đạo đức người lái xe.
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 3, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 3 AND trang_thai = 'approved';

    -- Nhóm 4: 01 câu / đề - Kỹ thuật lái xe hoặc cấu tạo sửa chữa.
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 4, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 4 AND trang_thai = 'approved';

    -- Nhóm 5: 08 câu / đề - Báo hiệu đường bộ.
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 5, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 5 AND trang_thai = 'approved';

    -- Nhóm 6: 06 câu / đề - Giải thế sa hình và kỹ năng xử lý tình huống giao thông.
    INSERT INTO @QuestionPool (nhom_cau_hoi, rn, cau_hoi_id)
    SELECT 6, ROW_NUMBER() OVER (ORDER BY id), id
    FROM cau_hoi
    WHERE chu_de_id = 6 AND trang_thai = 'approved';

    DECLARE @Topic1Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 1);
    DECLARE @Topic2Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 2);
    DECLARE @Topic3Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 3);
    DECLARE @Topic4Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 4);
    DECLARE @Topic5Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 5);
    DECLARE @Topic6Count INT = (SELECT COUNT(*) FROM @QuestionPool WHERE nhom_cau_hoi = 6);

    DECLARE @SetNo INT = 1;
    DECLARE @DeThiId BIGINT;

    DECLARE @CreatedExams TABLE
    (
        set_no INT PRIMARY KEY,
        de_thi_id BIGINT NOT NULL,
        ma_de_thi VARCHAR(30) NOT NULL,
        ten_de_thi NVARCHAR(150) NOT NULL
    );

    DECLARE @ExamSlots TABLE
    (
        nhom_cau_hoi INT NOT NULL,
        cau_bat_dau INT NOT NULL,
        so_cau INT NOT NULL,
        so_cau_hoi_hien_co INT NOT NULL,
        mo_ta NVARCHAR(200) NOT NULL,
        PRIMARY KEY (nhom_cau_hoi)
    );

    INSERT INTO @ExamSlots (nhom_cau_hoi, cau_bat_dau, so_cau, so_cau_hoi_hien_co, mo_ta)
    VALUES
        (1, 1, 8, @Topic1Count, N'Một số quy định chung và quy tắc giao thông đường bộ'),
        (2, 9, 1, @Topic2Count, N'Tình huống mất an toàn giao thông nghiêm trọng'),
        (3, 10, 1, @Topic3Count, N'Văn hóa giao thông, đạo đức người lái xe'),
        (4, 11, 1, @Topic4Count, N'Kỹ thuật lái xe hoặc cấu tạo sửa chữa'),
        (5, 12, 8, @Topic5Count, N'Báo hiệu đường bộ'),
        (6, 20, 6, @Topic6Count, N'Giải thế sa hình và kỹ năng xử lý tình huống giao thông');

    WHILE @SetNo <= 10
    BEGIN
        INSERT INTO de_thi
        (
            ma_de_thi,
            ten_de_thi,
            ky_thi_id,
            tong_so_cau,
            thoi_gian_lam_bai,
            trang_thai,
            loai_de_thi,
            nguoi_tao_id,
            ngay_tao
        )
        VALUES
        (
            CONCAT('MO_PHONG_A1A_SET_', RIGHT('0' + CAST(@SetNo AS VARCHAR(2)), 2)),
            N'Đề thi thử A1/A - 25 câu (Set ' + CAST(@SetNo AS NVARCHAR(10)) + N')',
            @KyThiId,
            25,
            19,
            'published',
            @LoaiDeThiThiThu,
            @NguoiTaoId,
            GETDATE()
        );

        SET @DeThiId = SCOPE_IDENTITY();

        INSERT INTO @CreatedExams (set_no, de_thi_id, ma_de_thi, ten_de_thi)
        VALUES
        (
            @SetNo,
            @DeThiId,
            CONCAT('MO_PHONG_A1A_SET_', RIGHT('0' + CAST(@SetNo AS VARCHAR(2)), 2)),
            N'Đề thi thử A1/A - 25 câu (Set ' + CAST(@SetNo AS NVARCHAR(10)) + N')'
        );

        -- Gán câu hỏi theo đúng cơ cấu 25 câu / đề.
        -- Cách chọn câu:
        -- - Nếu nhóm có đủ số câu cần dùng cho 10 đề thì lấy tuần tự, hạn chế trùng lặp.
        -- - Nếu nhóm có ít hơn số câu cần dùng cho 10 đề thì quay vòng bằng phép modulo để vẫn đủ slot.
        -- Nhờ vậy các câu đang approved trong từng nhóm đều được đưa vào pool sinh đề và được sử dụng nhiều nhất có thể.
        INSERT INTO de_thi_cau_hoi (de_thi_id, cau_hoi_id, thu_tu_cau)
        SELECT
            @DeThiId,
            qp.cau_hoi_id,
            es.cau_bat_dau + seq.n - 1 AS thu_tu_cau
        FROM @ExamSlots AS es
        CROSS APPLY
        (
            SELECT 1 AS n UNION ALL
            SELECT 2 UNION ALL
            SELECT 3 UNION ALL
            SELECT 4 UNION ALL
            SELECT 5 UNION ALL
            SELECT 6 UNION ALL
            SELECT 7 UNION ALL
            SELECT 8
        ) AS seq
        INNER JOIN @QuestionPool AS qp
            ON qp.nhom_cau_hoi = es.nhom_cau_hoi
           AND qp.rn = ((((@SetNo - 1) * es.so_cau + seq.n - 1) % es.so_cau_hoi_hien_co) + 1)
        WHERE seq.n <= es.so_cau;

        SET @SetNo += 1;
    END;

    -- Kiểm tra chắc chắn mỗi đề có đúng 25 câu.
    IF EXISTS
    (
        SELECT 1
        FROM @CreatedExams AS ce
        LEFT JOIN de_thi_cau_hoi AS dtch ON dtch.de_thi_id = ce.de_thi_id
        GROUP BY ce.de_thi_id
        HAVING COUNT(dtch.id) <> 25
    )
    BEGIN
        THROW 51007, N'Có đề thi thử không đủ đúng 25 câu.', 1;
    END;

    COMMIT TRANSACTION;

    SELECT
        ce.set_no,
        ce.de_thi_id,
        ce.ma_de_thi,
        ce.ten_de_thi,
        COUNT(dtch.id) AS tong_cau_hoi
    FROM @CreatedExams AS ce
    LEFT JOIN de_thi_cau_hoi AS dtch ON dtch.de_thi_id = ce.de_thi_id
    GROUP BY ce.set_no, ce.de_thi_id, ce.ma_de_thi, ce.ten_de_thi
    ORDER BY ce.set_no;

    SELECT
        es.nhom_cau_hoi,
        es.mo_ta,
        es.so_cau AS so_cau_moi_de,
        es.so_cau * 10 AS tong_slot_10_de,
        es.so_cau_hoi_hien_co,
        COUNT(DISTINCT dtch.cau_hoi_id) AS so_cau_hoi_da_dung_khong_trung,
        COUNT(dtch.cau_hoi_id) AS tong_luot_su_dung
    FROM @ExamSlots AS es
    LEFT JOIN @CreatedExams AS ce ON 1 = 1
    LEFT JOIN de_thi_cau_hoi AS dtch ON dtch.de_thi_id = ce.de_thi_id
    LEFT JOIN @QuestionPool AS qp ON qp.cau_hoi_id = dtch.cau_hoi_id AND qp.nhom_cau_hoi = es.nhom_cau_hoi
    WHERE qp.cau_hoi_id IS NOT NULL
    GROUP BY es.nhom_cau_hoi, es.mo_ta, es.so_cau, es.so_cau_hoi_hien_co
    ORDER BY es.nhom_cau_hoi;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
