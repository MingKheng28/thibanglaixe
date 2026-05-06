/*
    Script: 13_approve_all_student_registrations.sql
    Mục đích: Phê duyệt FULL tất cả học viên đã đăng ký mà chưa cần xây dựng giao diện phê duyệt.

    Script này xử lý:
      1. dang_ky_khoa_hoc  - duyệt đăng ký khóa học.
      2. lop_hoc_hoc_vien  - thêm học viên vào lớp tương ứng nếu chưa có.
      3. dang_ky_du_thi    - duyệt đăng ký dự thi.
      4. ho_so_dang_ky     - duyệt hồ sơ đăng ký.

    Cách gán lớp:
      - Vì bảng dang_ky_khoa_hoc hiện không lưu classId/lop_hoc_id,
        script sẽ tự chọn 1 lớp đang mở thuộc cùng khóa học cho từng đăng ký.
      - Ưu tiên lớp đang mở, chưa đủ sĩ số.
      - Nếu học viên đã có trong một lớp thuộc khóa học đó rồi thì giữ lớp hiện có.

    Ghi chú:
      - Không tự gán nguoi_duyet_id vì script không biết chính xác tài khoản admin/người duyệt hợp lệ.
      - Giữ nguyên nguoi_duyet_id hiện có nếu đã có dữ liệu.
*/

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @ApprovedAt DATETIME = GETDATE();
    DECLARE @ApprovedDate DATE = CONVERT(DATE, @ApprovedAt);

    IF OBJECT_ID('tempdb..#CourseRegistrationApproval') IS NOT NULL
        DROP TABLE #CourseRegistrationApproval;

    CREATE TABLE #CourseRegistrationApproval
    (
        registration_id BIGINT NOT NULL PRIMARY KEY,
        hoc_vien_id BIGINT NOT NULL,
        khoa_hoc_id BIGINT NOT NULL,
        lop_hoc_id BIGINT NULL
    );

    /*
        Gom toàn bộ đăng ký khóa học cần xử lý.
        Nếu học viên đã nằm trong một lớp của khóa học thì dùng lớp đó.
        Nếu chưa có lớp thì tự chọn một lớp đang mở, còn sĩ số trong cùng khóa học.
    */
    INSERT INTO #CourseRegistrationApproval
    (
        registration_id,
        hoc_vien_id,
        khoa_hoc_id,
        lop_hoc_id
    )
    SELECT
        dkkh.id,
        dkkh.hoc_vien_id,
        dkkh.khoa_hoc_id,
        COALESCE(existing_class.lop_hoc_id, available_class.lop_hoc_id) AS lop_hoc_id
    FROM dang_ky_khoa_hoc AS dkkh
    OUTER APPLY
    (
        SELECT TOP (1)
            lhhv.lop_hoc_id
        FROM lop_hoc_hoc_vien AS lhhv
        INNER JOIN lop_hoc AS lh
            ON lh.id = lhhv.lop_hoc_id
        WHERE lhhv.hoc_vien_id = dkkh.hoc_vien_id
          AND lh.khoa_hoc_id = dkkh.khoa_hoc_id
        ORDER BY
            CASE WHEN lhhv.trang_thai = 'dang_hoc' THEN 0 ELSE 1 END,
            lhhv.id DESC
    ) AS existing_class
    OUTER APPLY
    (
        SELECT TOP (1)
            lh.id AS lop_hoc_id
        FROM lop_hoc AS lh
        OUTER APPLY
        (
            SELECT COUNT(1) AS so_luong_dang_hoc
            FROM lop_hoc_hoc_vien AS lhhv_count
            WHERE lhhv_count.lop_hoc_id = lh.id
              AND lhhv_count.trang_thai = 'dang_hoc'
        ) AS class_size
        WHERE lh.khoa_hoc_id = dkkh.khoa_hoc_id
          AND NOT EXISTS
          (
              SELECT 1
              FROM lop_hoc_hoc_vien AS lhhv_same_class
              WHERE lhhv_same_class.lop_hoc_id = lh.id
                AND lhhv_same_class.hoc_vien_id = dkkh.hoc_vien_id
          )
          AND
          (
              lh.si_so_toi_da IS NULL
              OR lh.si_so_toi_da <= 0
              OR class_size.so_luong_dang_hoc < lh.si_so_toi_da
          )
        ORDER BY
            CASE WHEN lh.trang_thai = 'dang_mo' THEN 0 ELSE 1 END,
            class_size.so_luong_dang_hoc ASC,
            lh.ngay_bat_dau ASC,
            lh.id ASC
    ) AS available_class;

    /* Thêm học viên vào lớp cho tất cả đăng ký khóa học có lớp hợp lệ */
    INSERT INTO lop_hoc_hoc_vien
    (
        lop_hoc_id,
        hoc_vien_id,
        ngay_vao_lop,
        trang_thai
    )
    SELECT
        approval.lop_hoc_id,
        approval.hoc_vien_id,
        @ApprovedDate,
        'dang_hoc'
    FROM #CourseRegistrationApproval AS approval
    WHERE approval.lop_hoc_id IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM lop_hoc_hoc_vien AS existing_member
          WHERE existing_member.lop_hoc_id = approval.lop_hoc_id
            AND existing_member.hoc_vien_id = approval.hoc_vien_id
      );

    DECLARE @InsertedClassMembers INT = @@ROWCOUNT;

    /* Phê duyệt tất cả đăng ký khóa học của học viên nếu đăng ký có lớp hợp lệ */
    UPDATE dkkh
    SET
        dkkh.trang_thai = 'da_duyet',
        dkkh.ngay_duyet = ISNULL(dkkh.ngay_duyet, @ApprovedAt)
    FROM dang_ky_khoa_hoc AS dkkh
    INNER JOIN #CourseRegistrationApproval AS approval
        ON approval.registration_id = dkkh.id
    WHERE approval.lop_hoc_id IS NOT NULL
      AND
      (
          dkkh.trang_thai IS NULL
          OR dkkh.trang_thai <> 'da_duyet'
      );

    DECLARE @ApprovedCourseRegistrations INT = @@ROWCOUNT;

    /* Phê duyệt tất cả đăng ký dự thi của học viên */
    UPDATE dang_ky_du_thi
    SET
        trang_thai = 'da_duyet',
        ngay_duyet = ISNULL(ngay_duyet, @ApprovedAt)
    WHERE trang_thai IS NULL
       OR trang_thai <> 'da_duyet';

    DECLARE @ApprovedExamRegistrations INT = @@ROWCOUNT;

    /* Phê duyệt tất cả hồ sơ đăng ký của học viên */
    UPDATE ho_so_dang_ky
    SET
        trang_thai = 'da_duyet',
        ngay_nop = ISNULL(ngay_nop, @ApprovedAt),
        ngay_duyet = ISNULL(ngay_duyet, @ApprovedAt)
    WHERE trang_thai IS NULL
       OR trang_thai <> 'da_duyet';

    DECLARE @ApprovedProfiles INT = @@ROWCOUNT;

    DECLARE @SkippedCourseRegistrationsNoOpenClass INT =
    (
        SELECT COUNT(1)
        FROM #CourseRegistrationApproval
        WHERE lop_hoc_id IS NULL
    );

    COMMIT TRANSACTION;

    SELECT
        @ApprovedCourseRegistrations AS so_dang_ky_khoa_hoc_da_duyet,
        @InsertedClassMembers AS so_hoc_vien_duoc_them_vao_lop,
        @ApprovedExamRegistrations AS so_dang_ky_du_thi_da_duyet,
        @ApprovedProfiles AS so_ho_so_dang_ky_da_duyet,
        @SkippedCourseRegistrationsNoOpenClass AS so_dang_ky_khoa_hoc_chua_duyet_vi_khong_co_lop_dang_mo;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;