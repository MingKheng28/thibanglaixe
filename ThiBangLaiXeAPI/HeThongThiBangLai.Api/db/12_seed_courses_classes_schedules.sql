USE [he_thong_thi_bang_lai];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    /*
        Seed khóa học -> lớp học -> lịch học mẫu.
        Script này sẽ xóa dữ liệu liên quan trước để tránh lỗi FK, sau đó reset identity về 0.

        Thứ tự xóa:
        1. diem_danh: phụ thuộc buoi_hoc
        2. lop_hoc_hoc_vien: phụ thuộc lop_hoc
        3. dang_ky_khoa_hoc: phụ thuộc khoa_hoc
        4. buoi_hoc
        5. lop_hoc
        6. khoa_hoc
    */

    DELETE FROM dbo.diem_danh;
    DELETE FROM dbo.lop_hoc_hoc_vien;
    DELETE FROM dbo.dang_ky_khoa_hoc;
    DELETE FROM dbo.buoi_hoc;
    DELETE FROM dbo.lop_hoc;
    DELETE FROM dbo.khoa_hoc;

    DBCC CHECKIDENT ('dbo.diem_danh', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.lop_hoc_hoc_vien', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.dang_ky_khoa_hoc', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.buoi_hoc', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.lop_hoc', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('dbo.khoa_hoc', RESEED, 0) WITH NO_INFOMSGS;

    INSERT INTO dbo.khoa_hoc
    (
        ma_khoa_hoc,
        ten_khoa_hoc,
        mo_ta,
        hoc_phi,
        thoi_luong,
        trang_thai
    )
    VALUES
    ('A1-2026-05', N'Khóa A1 tháng 05/2026', N'Khóa học lái xe mô tô hạng A1, học lý thuyết luật giao thông và thực hành kỹ năng lái xe cơ bản.', 1200000, 12, 'dang_mo'),
    ('A1-2026-06', N'Khóa A1 tháng 06/2026', N'Khóa học lái xe mô tô hạng A1 dành cho học viên đăng ký thi tháng 06/2026.', 1200000, 12, 'dang_mo'),
    ('A1-2026-07', N'Khóa A1 tháng 07/2026', N'Khóa học lái xe mô tô hạng A1, lịch học buổi tối phù hợp người đi làm.', 1250000, 12, 'dang_mo'),
    ('A-2026-05',  N'Khóa A tháng 05/2026',  N'Khóa học lái xe mô tô hạng A, gồm lý thuyết và thực hành theo chương trình đào tạo chuẩn.', 1800000, 12, 'dang_mo'),
    ('A-2026-06',  N'Khóa A tháng 06/2026',  N'Khóa học lái xe mô tô hạng A dành cho học viên cần lịch học buổi tối.', 1800000, 12, 'dang_mo'),
    ('B1-2026-05', N'Khóa B1 tháng 05/2026', N'Khóa học lái xe ô tô hạng B1, đào tạo lý thuyết và thực hành lái xe cơ bản.', 9500000, 16, 'dang_mo'),
    ('B2-2026-05', N'Khóa B2 tháng 05/2026', N'Khóa học lái xe ô tô hạng B2, phù hợp học viên có nhu cầu lái xe dịch vụ.', 10500000, 16, 'dang_mo'),
    ('C-2026-05',  N'Khóa C tháng 05/2026',  N'Khóa học lái xe tải hạng C, đào tạo lý thuyết và thực hành nâng cao.', 14500000, 18, 'dang_mo');

    INSERT INTO dbo.lop_hoc
    (
        khoa_hoc_id,
        ma_lop,
        ten_lop,
        giao_vien_id,
        ngay_bat_dau,
        ngay_ket_thuc,
        si_so_toi_da,
        trang_thai
    )
    SELECT id, 'A1-T05-2026-TOI', N'Lớp A1 tháng 05/2026 ca tối', NULL, CONVERT(date, '2026-05-04'), CONVERT(date, '2026-05-29'), 30, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'A1-2026-05'
    UNION ALL
    SELECT id, 'A1-T06-2026-TOI', N'Lớp A1 tháng 06/2026 ca tối', NULL, CONVERT(date, '2026-06-01'), CONVERT(date, '2026-06-26'), 30, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'A1-2026-06'
    UNION ALL
    SELECT id, 'A1-T07-2026-TOI', N'Lớp A1 tháng 07/2026 ca tối', NULL, CONVERT(date, '2026-07-06'), CONVERT(date, '2026-07-31'), 30, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'A1-2026-07'
    UNION ALL
    SELECT id, 'A-T05-2026-TOI', N'Lớp A tháng 05/2026 ca tối', NULL, CONVERT(date, '2026-05-05'), CONVERT(date, '2026-05-30'), 25, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'A-2026-05'
    UNION ALL
    SELECT id, 'A-T06-2026-TOI', N'Lớp A tháng 06/2026 ca tối', NULL, CONVERT(date, '2026-06-02'), CONVERT(date, '2026-06-27'), 25, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'A-2026-06'
    UNION ALL
    SELECT id, 'B1-T05-2026-SANG', N'Lớp B1 tháng 05/2026 ca sáng', NULL, CONVERT(date, '2026-05-04'), CONVERT(date, '2026-06-08'), 20, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'B1-2026-05'
    UNION ALL
    SELECT id, 'B2-T05-2026-CHIEU', N'Lớp B2 tháng 05/2026 ca chiều', NULL, CONVERT(date, '2026-05-04'), CONVERT(date, '2026-06-08'), 20, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'B2-2026-05'
    UNION ALL
    SELECT id, 'C-T05-2026-CUOITUAN', N'Lớp C tháng 05/2026 cuối tuần', NULL, CONVERT(date, '2026-05-09'), CONVERT(date, '2026-06-20'), 18, 'dang_mo'
    FROM dbo.khoa_hoc WHERE ma_khoa_hoc = 'C-2026-05';

    DECLARE @Schedules TABLE
    (
        ma_lop varchar(30) NOT NULL,
        so_buoi int NOT NULL,
        ngay_hoc date NOT NULL,
        gio_bat_dau time NOT NULL,
        gio_ket_thuc time NOT NULL,
        phong_hoc nvarchar(100) NULL,
        noi_dung nvarchar(500) NULL
    );

    INSERT INTO @Schedules (ma_lop, so_buoi, ngay_hoc, gio_bat_dau, gio_ket_thuc, phong_hoc, noi_dung)
    VALUES
    -- A1 tháng 05/2026 - ca tối
    ('A1-T05-2026-TOI', 1,  '2026-05-04', '18:30', '20:30', N'Phòng LT1', N'Khai giảng, giới thiệu chương trình, quy định học tập'),
    ('A1-T05-2026-TOI', 2,  '2026-05-06', '18:30', '20:30', N'Phòng LT1', N'Hệ thống biển báo đường bộ'),
    ('A1-T05-2026-TOI', 3,  '2026-05-08', '18:30', '20:30', N'Phòng LT1', N'Quy tắc giao thông đường bộ'),
    ('A1-T05-2026-TOI', 4,  '2026-05-10', '18:30', '20:30', N'Sân tập A1', N'Thực hành bài số 8'),
    ('A1-T05-2026-TOI', 5,  '2026-05-12', '18:30', '20:30', N'Sân tập A1', N'Thực hành đường thẳng, đường vòng'),
    ('A1-T05-2026-TOI', 6,  '2026-05-14', '18:30', '20:30', N'Phòng LT1', N'Ôn tập câu hỏi điểm liệt'),
    ('A1-T05-2026-TOI', 7,  '2026-05-16', '18:30', '20:30', N'Sân tập A1', N'Thực hành tổng hợp'),
    ('A1-T05-2026-TOI', 8,  '2026-05-18', '18:30', '20:30', N'Phòng LT1', N'Làm đề thi thử'),
    ('A1-T05-2026-TOI', 9,  '2026-05-20', '18:30', '20:30', N'Sân tập A1', N'Sửa lỗi kỹ năng thực hành'),
    ('A1-T05-2026-TOI', 10, '2026-05-22', '18:30', '20:30', N'Phòng LT1', N'Ôn tập lý thuyết tổng hợp'),
    ('A1-T05-2026-TOI', 11, '2026-05-24', '18:30', '20:30', N'Sân tập A1', N'Thi thử thực hành'),
    ('A1-T05-2026-TOI', 12, '2026-05-26', '18:30', '20:30', N'Phòng LT1', N'Tổng kết khóa học'),

    -- A1 tháng 06/2026 - ca tối
    ('A1-T06-2026-TOI', 1,  '2026-06-01', '18:30', '20:30', N'Phòng LT1', N'Khai giảng, giới thiệu chương trình'),
    ('A1-T06-2026-TOI', 2,  '2026-06-03', '18:30', '20:30', N'Phòng LT1', N'Hệ thống biển báo đường bộ'),
    ('A1-T06-2026-TOI', 3,  '2026-06-05', '18:30', '20:30', N'Phòng LT1', N'Quy tắc giao thông đường bộ'),
    ('A1-T06-2026-TOI', 4,  '2026-06-07', '18:30', '20:30', N'Sân tập A1', N'Thực hành bài số 8'),
    ('A1-T06-2026-TOI', 5,  '2026-06-09', '18:30', '20:30', N'Sân tập A1', N'Thực hành kỹ năng cơ bản'),
    ('A1-T06-2026-TOI', 6,  '2026-06-11', '18:30', '20:30', N'Phòng LT1', N'Ôn tập câu hỏi điểm liệt'),
    ('A1-T06-2026-TOI', 7,  '2026-06-13', '18:30', '20:30', N'Sân tập A1', N'Thực hành tổng hợp'),
    ('A1-T06-2026-TOI', 8,  '2026-06-15', '18:30', '20:30', N'Phòng LT1', N'Làm đề thi thử'),
    ('A1-T06-2026-TOI', 9,  '2026-06-17', '18:30', '20:30', N'Sân tập A1', N'Sửa lỗi kỹ năng thực hành'),
    ('A1-T06-2026-TOI', 10, '2026-06-19', '18:30', '20:30', N'Phòng LT1', N'Ôn tập lý thuyết tổng hợp'),
    ('A1-T06-2026-TOI', 11, '2026-06-21', '18:30', '20:30', N'Sân tập A1', N'Thi thử thực hành'),
    ('A1-T06-2026-TOI', 12, '2026-06-23', '18:30', '20:30', N'Phòng LT1', N'Tổng kết khóa học'),

    -- A1 tháng 07/2026 - ca tối
    ('A1-T07-2026-TOI', 1,  '2026-07-06', '18:30', '20:30', N'Phòng LT1', N'Khai giảng, giới thiệu chương trình'),
    ('A1-T07-2026-TOI', 2,  '2026-07-08', '18:30', '20:30', N'Phòng LT1', N'Hệ thống biển báo đường bộ'),
    ('A1-T07-2026-TOI', 3,  '2026-07-10', '18:30', '20:30', N'Phòng LT1', N'Quy tắc giao thông đường bộ'),
    ('A1-T07-2026-TOI', 4,  '2026-07-12', '18:30', '20:30', N'Sân tập A1', N'Thực hành bài số 8'),
    ('A1-T07-2026-TOI', 5,  '2026-07-14', '18:30', '20:30', N'Sân tập A1', N'Thực hành kỹ năng cơ bản'),
    ('A1-T07-2026-TOI', 6,  '2026-07-16', '18:30', '20:30', N'Phòng LT1', N'Ôn tập câu hỏi điểm liệt'),
    ('A1-T07-2026-TOI', 7,  '2026-07-18', '18:30', '20:30', N'Sân tập A1', N'Thực hành tổng hợp'),
    ('A1-T07-2026-TOI', 8,  '2026-07-20', '18:30', '20:30', N'Phòng LT1', N'Làm đề thi thử'),
    ('A1-T07-2026-TOI', 9,  '2026-07-22', '18:30', '20:30', N'Sân tập A1', N'Sửa lỗi kỹ năng thực hành'),
    ('A1-T07-2026-TOI', 10, '2026-07-24', '18:30', '20:30', N'Phòng LT1', N'Ôn tập lý thuyết tổng hợp'),
    ('A1-T07-2026-TOI', 11, '2026-07-26', '18:30', '20:30', N'Sân tập A1', N'Thi thử thực hành'),
    ('A1-T07-2026-TOI', 12, '2026-07-28', '18:30', '20:30', N'Phòng LT1', N'Tổng kết khóa học'),

    -- A tháng 05/2026 - ca tối
    ('A-T05-2026-TOI', 1,  '2026-05-05', '18:30', '20:30', N'Phòng LT2', N'Khai giảng, phổ biến quy chế'),
    ('A-T05-2026-TOI', 2,  '2026-05-07', '18:30', '20:30', N'Phòng LT2', N'Lý thuyết luật giao thông'),
    ('A-T05-2026-TOI', 3,  '2026-05-09', '18:30', '20:30', N'Phòng LT2', N'Biển báo và sa hình'),
    ('A-T05-2026-TOI', 4,  '2026-05-11', '18:30', '20:30', N'Sân tập A', N'Thực hành kỹ năng điều khiển xe'),
    ('A-T05-2026-TOI', 5,  '2026-05-13', '18:30', '20:30', N'Sân tập A', N'Thực hành bài thi tổng hợp'),
    ('A-T05-2026-TOI', 6,  '2026-05-15', '18:30', '20:30', N'Phòng LT2', N'Ôn tập câu hỏi điểm liệt'),
    ('A-T05-2026-TOI', 7,  '2026-05-17', '18:30', '20:30', N'Sân tập A', N'Thực hành nâng cao'),
    ('A-T05-2026-TOI', 8,  '2026-05-19', '18:30', '20:30', N'Phòng LT2', N'Làm đề thi thử'),
    ('A-T05-2026-TOI', 9,  '2026-05-21', '18:30', '20:30', N'Sân tập A', N'Sửa lỗi thực hành'),
    ('A-T05-2026-TOI', 10, '2026-05-23', '18:30', '20:30', N'Phòng LT2', N'Ôn tập lý thuyết tổng hợp'),
    ('A-T05-2026-TOI', 11, '2026-05-25', '18:30', '20:30', N'Sân tập A', N'Thi thử thực hành'),
    ('A-T05-2026-TOI', 12, '2026-05-27', '18:30', '20:30', N'Phòng LT2', N'Tổng kết khóa học'),

    -- A tháng 06/2026 - ca tối
    ('A-T06-2026-TOI', 1,  '2026-06-02', '18:30', '20:30', N'Phòng LT2', N'Khai giảng, phổ biến quy chế'),
    ('A-T06-2026-TOI', 2,  '2026-06-04', '18:30', '20:30', N'Phòng LT2', N'Lý thuyết luật giao thông'),
    ('A-T06-2026-TOI', 3,  '2026-06-06', '18:30', '20:30', N'Phòng LT2', N'Biển báo và sa hình'),
    ('A-T06-2026-TOI', 4,  '2026-06-08', '18:30', '20:30', N'Sân tập A', N'Thực hành kỹ năng điều khiển xe'),
    ('A-T06-2026-TOI', 5,  '2026-06-10', '18:30', '20:30', N'Sân tập A', N'Thực hành bài thi tổng hợp'),
    ('A-T06-2026-TOI', 6,  '2026-06-12', '18:30', '20:30', N'Phòng LT2', N'Ôn tập câu hỏi điểm liệt'),
    ('A-T06-2026-TOI', 7,  '2026-06-14', '18:30', '20:30', N'Sân tập A', N'Thực hành nâng cao'),
    ('A-T06-2026-TOI', 8,  '2026-06-16', '18:30', '20:30', N'Phòng LT2', N'Làm đề thi thử'),
    ('A-T06-2026-TOI', 9,  '2026-06-18', '18:30', '20:30', N'Sân tập A', N'Sửa lỗi thực hành'),
    ('A-T06-2026-TOI', 10, '2026-06-20', '18:30', '20:30', N'Phòng LT2', N'Ôn tập lý thuyết tổng hợp'),
    ('A-T06-2026-TOI', 11, '2026-06-22', '18:30', '20:30', N'Sân tập A', N'Thi thử thực hành'),
    ('A-T06-2026-TOI', 12, '2026-06-24', '18:30', '20:30', N'Phòng LT2', N'Tổng kết khóa học'),

    -- B1 tháng 05/2026 - ca sáng
    ('B1-T05-2026-SANG', 1,  '2026-05-04', '07:30', '09:30', N'Phòng LT3', N'Khai giảng, giới thiệu chương trình B1'),
    ('B1-T05-2026-SANG', 2,  '2026-05-06', '07:30', '09:30', N'Phòng LT3', N'Lý thuyết luật giao thông'),
    ('B1-T05-2026-SANG', 3,  '2026-05-08', '07:30', '09:30', N'Phòng LT3', N'Biển báo và sa hình'),
    ('B1-T05-2026-SANG', 4,  '2026-05-11', '07:30', '09:30', N'Sân ô tô 1', N'Thực hành làm quen xe'),
    ('B1-T05-2026-SANG', 5,  '2026-05-13', '07:30', '09:30', N'Sân ô tô 1', N'Thực hành xuất phát và dừng xe'),
    ('B1-T05-2026-SANG', 6,  '2026-05-15', '07:30', '09:30', N'Sân ô tô 1', N'Thực hành sa hình'),
    ('B1-T05-2026-SANG', 7,  '2026-05-18', '07:30', '09:30', N'Phòng LT3', N'Làm đề thi thử lý thuyết'),
    ('B1-T05-2026-SANG', 8,  '2026-05-20', '07:30', '09:30', N'Sân ô tô 1', N'Thực hành đường trường'),
    ('B1-T05-2026-SANG', 9,  '2026-05-22', '07:30', '09:30', N'Sân ô tô 1', N'Sửa lỗi thực hành'),
    ('B1-T05-2026-SANG', 10, '2026-05-25', '07:30', '09:30', N'Phòng LT3', N'Ôn tập lý thuyết tổng hợp'),
    ('B1-T05-2026-SANG', 11, '2026-05-27', '07:30', '09:30', N'Sân ô tô 1', N'Thi thử thực hành'),
    ('B1-T05-2026-SANG', 12, '2026-05-29', '07:30', '09:30', N'Phòng LT3', N'Tổng kết giai đoạn 1'),
    ('B1-T05-2026-SANG', 13, '2026-06-01', '07:30', '09:30', N'Sân ô tô 1', N'Luyện tập tổng hợp'),
    ('B1-T05-2026-SANG', 14, '2026-06-03', '07:30', '09:30', N'Sân ô tô 1', N'Thi thử cuối khóa'),
    ('B1-T05-2026-SANG', 15, '2026-06-05', '07:30', '09:30', N'Phòng LT3', N'Ôn tập cuối khóa'),
    ('B1-T05-2026-SANG', 16, '2026-06-08', '07:30', '09:30', N'Phòng LT3', N'Tổng kết khóa học'),

    -- B2 tháng 05/2026 - ca chiều
    ('B2-T05-2026-CHIEU', 1,  '2026-05-04', '14:00', '16:00', N'Phòng LT4', N'Khai giảng, giới thiệu chương trình B2'),
    ('B2-T05-2026-CHIEU', 2,  '2026-05-06', '14:00', '16:00', N'Phòng LT4', N'Lý thuyết luật giao thông'),
    ('B2-T05-2026-CHIEU', 3,  '2026-05-08', '14:00', '16:00', N'Phòng LT4', N'Biển báo và sa hình'),
    ('B2-T05-2026-CHIEU', 4,  '2026-05-11', '14:00', '16:00', N'Sân ô tô 2', N'Thực hành làm quen xe'),
    ('B2-T05-2026-CHIEU', 5,  '2026-05-13', '14:00', '16:00', N'Sân ô tô 2', N'Thực hành xuất phát và dừng xe'),
    ('B2-T05-2026-CHIEU', 6,  '2026-05-15', '14:00', '16:00', N'Sân ô tô 2', N'Thực hành sa hình'),
    ('B2-T05-2026-CHIEU', 7,  '2026-05-18', '14:00', '16:00', N'Phòng LT4', N'Làm đề thi thử lý thuyết'),
    ('B2-T05-2026-CHIEU', 8,  '2026-05-20', '14:00', '16:00', N'Sân ô tô 2', N'Thực hành đường trường'),
    ('B2-T05-2026-CHIEU', 9,  '2026-05-22', '14:00', '16:00', N'Sân ô tô 2', N'Sửa lỗi thực hành'),
    ('B2-T05-2026-CHIEU', 10, '2026-05-25', '14:00', '16:00', N'Phòng LT4', N'Ôn tập lý thuyết tổng hợp'),
    ('B2-T05-2026-CHIEU', 11, '2026-05-27', '14:00', '16:00', N'Sân ô tô 2', N'Thi thử thực hành'),
    ('B2-T05-2026-CHIEU', 12, '2026-05-29', '14:00', '16:00', N'Phòng LT4', N'Tổng kết giai đoạn 1'),
    ('B2-T05-2026-CHIEU', 13, '2026-06-01', '14:00', '16:00', N'Sân ô tô 2', N'Luyện tập tổng hợp'),
    ('B2-T05-2026-CHIEU', 14, '2026-06-03', '14:00', '16:00', N'Sân ô tô 2', N'Thi thử cuối khóa'),
    ('B2-T05-2026-CHIEU', 15, '2026-06-05', '14:00', '16:00', N'Phòng LT4', N'Ôn tập cuối khóa'),
    ('B2-T05-2026-CHIEU', 16, '2026-06-08', '14:00', '16:00', N'Phòng LT4', N'Tổng kết khóa học'),

    -- C tháng 05/2026 - cuối tuần
    ('C-T05-2026-CUOITUAN', 1,  '2026-05-09', '08:00', '11:00', N'Phòng LT5', N'Khai giảng, giới thiệu chương trình hạng C'),
    ('C-T05-2026-CUOITUAN', 2,  '2026-05-10', '08:00', '11:00', N'Phòng LT5', N'Lý thuyết luật giao thông'),
    ('C-T05-2026-CUOITUAN', 3,  '2026-05-16', '08:00', '11:00', N'Phòng LT5', N'Biển báo và sa hình'),
    ('C-T05-2026-CUOITUAN', 4,  '2026-05-17', '08:00', '11:00', N'Sân tải 1', N'Thực hành làm quen xe tải'),
    ('C-T05-2026-CUOITUAN', 5,  '2026-05-23', '08:00', '11:00', N'Sân tải 1', N'Thực hành xuất phát và dừng xe'),
    ('C-T05-2026-CUOITUAN', 6,  '2026-05-24', '08:00', '11:00', N'Sân tải 1', N'Thực hành sa hình'),
    ('C-T05-2026-CUOITUAN', 7,  '2026-05-30', '08:00', '11:00', N'Phòng LT5', N'Làm đề thi thử lý thuyết'),
    ('C-T05-2026-CUOITUAN', 8,  '2026-05-31', '08:00', '11:00', N'Sân tải 1', N'Thực hành đường trường'),
    ('C-T05-2026-CUOITUAN', 9,  '2026-06-06', '08:00', '11:00', N'Sân tải 1', N'Sửa lỗi thực hành'),
    ('C-T05-2026-CUOITUAN', 10, '2026-06-07', '08:00', '11:00', N'Phòng LT5', N'Ôn tập lý thuyết tổng hợp'),
    ('C-T05-2026-CUOITUAN', 11, '2026-06-13', '08:00', '11:00', N'Sân tải 1', N'Luyện tập tổng hợp'),
    ('C-T05-2026-CUOITUAN', 12, '2026-06-14', '08:00', '11:00', N'Sân tải 1', N'Thi thử thực hành'),
    ('C-T05-2026-CUOITUAN', 13, '2026-06-20', '08:00', '11:00', N'Phòng LT5', N'Tổng kết khóa học');

    INSERT INTO dbo.buoi_hoc
    (
        lop_hoc_id,
        ten_buoi,
        ngay_hoc,
        gio_bat_dau,
        gio_ket_thuc,
        noi_dung,
        phong_hoc
    )
    SELECT
        lh.id,
        CONCAT(N'Buổi ', schedule.so_buoi),
        schedule.ngay_hoc,
        schedule.gio_bat_dau,
        schedule.gio_ket_thuc,
        schedule.noi_dung,
        schedule.phong_hoc
    FROM @Schedules AS schedule
    INNER JOIN dbo.lop_hoc AS lh ON lh.ma_lop = schedule.ma_lop
    ORDER BY lh.id, schedule.so_buoi;

    COMMIT TRANSACTION;

    SELECT N'Đã seed khóa học, lớp học và lịch học mẫu thành công' AS message;
    SELECT 'khoa_hoc' AS bang, COUNT(*) AS so_dong FROM dbo.khoa_hoc
    UNION ALL SELECT 'lop_hoc', COUNT(*) FROM dbo.lop_hoc
    UNION ALL SELECT 'buoi_hoc', COUNT(*) FROM dbo.buoi_hoc;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO
