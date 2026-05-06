/*
    Script: 14_seed_tuition_fee_type.sql
    Mục đích: Bổ sung cấu hình loại khoản thu học phí để backend tạo đơn thanh toán ZaloPay.

    Lỗi xử lý:
      - API POST /api/v1/payments/zalopay/create-order trả lỗi:
        "Chưa cấu hình loại khoản thu học phí"

    Nguyên nhân:
      - Bảng loai_khoan_thu chưa có dữ liệu loại khoản thu học phí.
      - ZaloPayPaymentService cần một dòng trong loai_khoan_thu để tạo chi_tiet_phieu_thu.
*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM loai_khoan_thu
        WHERE ma_loai = 'HOC_PHI'
    )
    BEGIN
        INSERT INTO loai_khoan_thu
        (
            ma_loai,
            ten_loai,
            so_tien_mac_dinh,
            mo_ta,
            trang_thai
        )
        VALUES
        (
            'HOC_PHI',
            N'Học phí khóa học',
            0,
            N'Khoản thu học phí dùng cho thanh toán khóa học qua ZaloPay',
            'hoat_dong'
        );
    END
    ELSE
    BEGIN
        UPDATE loai_khoan_thu
        SET
            ten_loai = CASE
                WHEN ten_loai IS NULL OR LTRIM(RTRIM(ten_loai)) = '' THEN N'Học phí khóa học'
                ELSE ten_loai
            END,
            trang_thai = 'hoat_dong'
        WHERE ma_loai = 'HOC_PHI';
    END

    COMMIT TRANSACTION;

    SELECT
        id,
        ma_loai,
        ten_loai,
        so_tien_mac_dinh,
        mo_ta,
        trang_thai
    FROM loai_khoan_thu
    WHERE ma_loai = 'HOC_PHI';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
