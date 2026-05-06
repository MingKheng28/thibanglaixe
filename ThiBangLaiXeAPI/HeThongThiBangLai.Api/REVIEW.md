# Review database/API/admin flow - hệ thống thi bằng lái xe

## 1. Kết luận nhanh

Hệ thống đã có nền tảng dữ liệu khá rộng: người dùng, vai trò/quyền, học viên, hồ sơ đăng ký, khóa học, lớp học, buổi học, điểm danh, câu hỏi, đáp án, đề thi, bài thi, kết quả thi, chứng chỉ, phiếu thu và loại khoản thu. Backend admin hiện đã có một số CRUD lõi cho khóa học/lớp học/lịch học/câu hỏi/đáp án/đề thi.

Tuy nhiên, hệ thống chưa hoàn chỉnh theo luồng vận hành trung tâm đào tạo GPLX vì còn thiếu các phần quan trọng:

- Chưa có bảng hồ sơ giáo viên riêng.
- Chưa có bảng thông báo/cảnh báo nghiệp vụ cho admin/giáo viên/học viên.
- Mã khóa học và mã lớp học đang chỉ là chuỗi tự do; chưa có chuẩn sinh mã/quan hệ mã đủ chặt.
- Lịch học chưa phân biệt lý thuyết/thực hành/lịch thi một cách chuẩn hóa.
- Giáo trình/chủ đề mới chỉ có `chu_de_cau_hoi`, chưa đủ cho curriculum đào tạo.
- FE admin mới hiển thị và tạo một phần nhỏ, chưa đủ CRUD thực tế.
- Quyền giáo viên hiện mới nằm ở role người dùng, chưa có boundary nghiệp vụ cho giáo viên.
- Duyệt học viên vào lớp chưa kiểm tra đã đóng học phí đầy đủ.
- Kết quả thi và đề thi có bảng/API nhưng dữ liệu dashboard có thể rỗng do seed/luồng ghi kết quả chưa đồng bộ giữa `bai_thi` và `exam_results`.

## 2. Hiện trạng schema chính

### 2.1. Nhóm tài khoản và phân quyền

Đang có:

- `nguoi_dung`
- `vai_tro`
- `quyen_han`
- `nguoi_dung_vai_tro`
- `vai_tro_quyen_han`
- `loai_nguoi_dung`
- `nguoi_dung_loai`

Đánh giá:

- Phù hợp làm authentication/authorization nền.
- Chưa đủ làm hồ sơ nghiệp vụ cho giáo viên vì `nguoi_dung` chỉ chứa username/email/phone/status.
- Việc lấy tên giáo viên bằng `hoc_vien.ho_ten` trong service là sai mô hình: giáo viên không nên phụ thuộc bảng học viên.

### 2.2. Nhóm học viên/hồ sơ/lớp

Đang có:

- `hoc_vien`
- `ho_so_dang_ky`
- `giay_to_dinh_kem`
- `dang_ky_khoa_hoc`
- `lop_hoc_hoc_vien`

Đánh giá:

- Học viên có hồ sơ riêng là đúng.
- Đăng ký khóa học và gán vào lớp đang tách riêng là hợp lý.
- Thiếu trạng thái thanh toán liên kết trực tiếp với đăng ký khóa học/lớp học.
- Duyệt vào lớp hiện có thể bỏ qua điều kiện học phí.

### 2.3. Nhóm khóa học/lớp/lịch học

Đang có:

- `khoa_hoc`
- `lop_hoc`
- `buoi_hoc`
- `diem_danh`

Đánh giá:

- `khoa_hoc.ma_khoa_hoc` và `lop_hoc.ma_lop` đều unique nhưng chưa có chuẩn format.
- `lop_hoc.giao_vien_id` đang FK sang `nguoi_dung`, không sang bảng giáo viên nghiệp vụ.
- `buoi_hoc` chưa có cột loại buổi: lý thuyết/thực hành/ôn tập/thi thử/lịch thi.
- Chưa có bảng tài nguyên phương tiện/sân tập/xe tập lái để phục vụ lịch thực hành.

### 2.4. Nhóm câu hỏi/chủ đề/đề thi/bài thi/kết quả

Đang có:

- `chu_de_cau_hoi`
- `cau_hoi`
- `dap_an`
- `de_thi`
- `de_thi_cau_hoi`
- `ky_thi`
- `ca_thi`
- `dang_ky_du_thi`
- `bai_thi`
- `chi_tiet_bai_thi`
- `exam_results`
- `certificates`

Đánh giá:

- Question bank đã có schema tốt ở mức cơ bản.
- `de_thi.loai_de_thi` được bổ sung qua SQL script, phù hợp tách đề thi thử/đề thật.
- `exam_results` tồn tại nhưng cần kiểm tra luồng ghi dữ liệu sau khi submit; nếu chỉ ghi vào `bai_thi` mà không insert `exam_results`, dashboard kết quả sẽ rỗng.
- `ky_thi`/`ca_thi` có dữ liệu nhưng FE/admin mới hiển thị, chưa CRUD đầy đủ.

### 2.5. Nhóm học phí/thanh toán

Đang có:

- `phieu_thu`
- `chi_tiet_phieu_thu`
- `loai_khoan_thu`
- ZaloPay endpoints tạo order/callback/status.

Đánh giá:

- Nền tảng thanh toán có nhưng chưa liên kết chặt với điều kiện duyệt lớp.
- Cần thêm quan hệ từ phiếu thu hoặc chi tiết phiếu thu đến `dang_ky_khoa_hoc` / `lop_hoc_hoc_vien` / `khoa_hoc` để xác định học phí nào thuộc đăng ký nào.

## 3. Review API hiện tại

### 3.1. Admin API đã có

Backend admin hiện có:

- Users: list, đổi trạng thái, gán/gỡ role.
- Questions: list/create/update/delete.
- Answers: create/update/delete.
- Exams: list/create/update/delete, add question to exam.
- Courses: list/create/update/delete.
- Classes: list/create/update/delete.
- Schedules: list/create/update/delete.
- Students: list only.
- Teachers: list only, dựa trên role user.
- Topics: list only.
- Exam periods: list only.
- Exam registrations: list/approve.
- Exam results: list only.
- Certificates: list only.
- Fee types: list only.
- Receipts: list/confirm/cancel.
- Course registrations: list/approve.
- Approve student into class.

Đánh giá:

- API admin đã có CRUD một phần, nhưng chưa đủ chuẩn quản trị.
- Các endpoint CRUD đang viết trực tiếp trong `AdminController`, file quá lớn và thiếu service/validator/transaction rõ ràng.
- Chưa có phân quyền chi tiết theo permission; chỉ `[Authorize]`, nghĩa là user đăng nhập bất kỳ nếu gọi được admin route có thể nguy hiểm nếu middleware chưa chặn role ở nơi khác.

### 3.2. API public/student đã có

Đang có:

- Courses: list/detail/classes/register/my registrations/approve.
- Questions: list/with-answers/detail/create/update/approve/archive/delete.
- Mock exams: list/detail/start/session/question/answer/submit/result/review.
- Payments ZaloPay: create order/status/callback.
- Wrong questions: summary/practice/resolved/delete.
- Auth: register/login/profile/change password.

Đánh giá:

- Flow thi thử đã tương đối hoàn chỉnh.
- Route naming trong documentation FE còn lệch: README nhắc `sample-exams`, trong code có `mock-exams`.
- Question API có CRUD nhưng dashboard đang gọi admin API list raw; nên thống nhất DTO và endpoint quản trị.

## 4. Review frontend admin

Admin dashboard hiện đã gọi các endpoint:

- users/students/teachers/courses/classes/schedules/topics/questions/exams/exam-periods/exam-results/certificates/receipts/course-registrations/exam-registrations/fee-types.

FE hiện có:

- Hiển thị bảng dữ liệu cho nhiều module.
- Tạo mới course/class/schedule/exam.
- Xóa course.
- Duyệt đăng ký khóa học/dự thi.
- Xác nhận/hủy phiếu thu.
- Đổi trạng thái user.

Thiếu ở FE:

- CRUD học viên.
- CRUD giáo viên.
- Update/delete đầy đủ khóa học/lớp/lịch học.
- CRUD chủ đề/giáo trình.
- CRUD câu hỏi + đáp án trong UI.
- CRUD đề thi + quản lý câu hỏi trong đề.
- CRUD kỳ thi/ca thi/đăng ký thi.
- CRUD kết quả thi/chứng chỉ.
- CRUD phiếu thu/chi tiết phiếu thu/loại khoản thu.
- Filter/search/pagination thực tế.
- Form validation và modal edit/delete có xác nhận chuẩn.

## 5. Trả lời 10 vấn đề cụ thể

### 5.1. Tạo bảng `giao_vien`

Nhận định: Nên tạo bảng giáo viên riêng.

Hiện tại `lop_hoc.giao_vien_id` và `diem_danh.giao_vien_id` đang FK sang `nguoi_dung`. API giáo viên chỉ lấy username/email/phone nên không có họ tên chuẩn. Service khóa học còn fallback sang `hoc_vien.ho_ten`, đây là dấu hiệu sai mô hình.

Đề xuất bảng:

```sql
CREATE TABLE giao_vien (
    id BIGINT IDENTITY PRIMARY KEY,
    nguoi_dung_id BIGINT NOT NULL UNIQUE,
    ma_giao_vien VARCHAR(30) NOT NULL UNIQUE,
    ho_ten NVARCHAR(150) NOT NULL,
    ngay_sinh DATE NULL,
    gioi_tinh NVARCHAR(10) NULL,
    cccd VARCHAR(20) NULL UNIQUE,
    so_gplx VARCHAR(50) NULL,
    hang_gplx NVARCHAR(20) NULL,
    chuyen_mon NVARCHAR(255) NULL,
    kinh_nghiem_nam INT NULL,
    trang_thai VARCHAR(30) NOT NULL DEFAULT 'hoat_dong',
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_giao_vien_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES nguoi_dung(id)
);
```

Migration:

1. Insert giáo viên từ `nguoi_dung` có role `giao_vien`/`teacher`.
2. Sinh `ma_giao_vien` dạng `GV000001`.
3. Tạm lấy `ho_ten = ten_dang_nhap` nếu chưa có dữ liệu, sau đó cho admin cập nhật.
4. Thêm `giao_vien_id` mới trong `lop_hoc` trỏ sang `giao_vien(id)`.
5. Backfill từ user id cũ sang teacher id mới.
6. Sau khi ổn định, rename/deprecate cột FK cũ.

### 5.2. Tạo bảng thông báo/cảnh báo

Nhận định: Nên tạo 2 lớp: thông báo nghiệp vụ và cảnh báo dashboard.

Đề xuất tối thiểu:

```sql
CREATE TABLE thong_bao (
    id BIGINT IDENTITY PRIMARY KEY,
    loai VARCHAR(50) NOT NULL,
    muc_do VARCHAR(20) NOT NULL DEFAULT 'info',
    tieu_de NVARCHAR(255) NOT NULL,
    noi_dung NVARCHAR(MAX) NULL,
    doi_tuong VARCHAR(30) NOT NULL, -- admin, giao_vien, hoc_vien, all
    nguoi_nhan_id BIGINT NULL,
    entity_type VARCHAR(50) NULL,
    entity_id BIGINT NULL,
    da_doc BIT NOT NULL DEFAULT 0,
    trang_thai VARCHAR(30) NOT NULL DEFAULT 'active',
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    read_at DATETIME2 NULL
);
```

Các cảnh báo nên sinh tự động:

- Hồ sơ chờ duyệt quá N ngày.
- Phiếu thu chờ xác nhận.
- Đăng ký khóa học đã thanh toán nhưng chưa vào lớp.
- Lớp sắp khai giảng chưa đủ giáo viên/lịch học.
- Kỳ thi/ca thi sắp diễn ra nhưng thiếu đề thi.
- Kết quả thi chưa xác nhận.
- Học viên trượt/vi phạm quy chế.

### 5.3. Chuẩn hóa mã khóa học và mã lớp học

Hiện tại:

- `khoa_hoc.ma_khoa_hoc` unique.
- `lop_hoc.ma_lop` unique.
- Không có constraint format.
- Mã lớp không bắt buộc chứa mã khóa học.

Đề xuất:

- Khóa học: `KH-A1-2026-001` hoặc `A1-2026-001`.
- Lớp học: `{ma_khoa_hoc}-L{sequence}` ví dụ `A1-2026-001-L01`.
- Thêm cột `so_thu_tu` trong `lop_hoc` theo khóa học.
- Unique composite: `(khoa_hoc_id, so_thu_tu)`.
- Giữ `ma_lop` unique nhưng sinh từ service, không nhập tay tự do.

DB nên thêm:

```sql
ALTER TABLE lop_hoc ADD so_thu_tu INT NULL;
CREATE UNIQUE INDEX uq_lop_hoc_khoa_hoc_stt ON lop_hoc(khoa_hoc_id, so_thu_tu) WHERE so_thu_tu IS NOT NULL;
```

Trong source, không để FE tự quyết định code; backend sinh mã qua `CodeGeneratorService`.

### 5.4. CRUD học viên/giáo viên/khóa học/lớp/lịch học và tách lý thuyết/thực hành

BE:

- Khóa học/lớp/lịch học đã có CRUD admin cơ bản.
- Học viên chỉ list, chưa CRUD/admin update profile/status.
- Giáo viên chỉ list, chưa CRUD.

FE:

- Mới create course/class/schedule/exam và delete course.
- Chưa đủ CRUD.

Đề xuất DB lịch học:

```sql
ALTER TABLE buoi_hoc ADD loai_buoi VARCHAR(30) NOT NULL DEFAULT 'ly_thuyet';
ALTER TABLE buoi_hoc ADD dia_diem NVARCHAR(255) NULL;
ALTER TABLE buoi_hoc ADD giao_vien_id BIGINT NULL;
ALTER TABLE buoi_hoc ADD xe_tap_lai_id BIGINT NULL;
```

Chuẩn enum:

- `ly_thuyet`
- `thuc_hanh`
- `on_tap`
- `thi_thu`
- `thi_sat_hach`

Nếu làm thực hành tốt hơn, thêm:

- `phuong_tien`
- `san_tap`
- `lich_xe_tap`

API cần:

- `GET/POST/PUT/DELETE /api/v1/admin/students`
- `GET/POST/PUT/DELETE /api/v1/admin/teachers`
- `GET/PUT/DELETE /api/v1/admin/schedules/{id}` đã có, nhưng cần field `loai_buoi`.
- `GET /api/v1/admin/classes/{id}/students`
- `POST /api/v1/admin/classes/{id}/students/{studentId}` có kiểm tra học phí.

### 5.5. Giáo trình & chủ đề chưa có CRUD

Hiện tại chỉ có `chu_de_cau_hoi`, phù hợp phân loại câu hỏi, chưa phải giáo trình đào tạo.

Đề xuất tách:

- `giao_trinh`: giáo trình/khung chương trình.
- `bai_hoc`: bài học thuộc giáo trình.
- `chu_de_cau_hoi`: topic của question bank, có thể link với `bai_hoc`.

DB đề xuất:

```sql
CREATE TABLE giao_trinh (
    id BIGINT IDENTITY PRIMARY KEY,
    ma_giao_trinh VARCHAR(30) NOT NULL UNIQUE,
    ten_giao_trinh NVARCHAR(150) NOT NULL,
    hang_bang NVARCHAR(20) NULL,
    mo_ta NVARCHAR(500) NULL,
    trang_thai VARCHAR(30) NOT NULL DEFAULT 'active'
);

CREATE TABLE bai_hoc (
    id BIGINT IDENTITY PRIMARY KEY,
    giao_trinh_id BIGINT NOT NULL,
    ma_bai_hoc VARCHAR(30) NOT NULL,
    ten_bai_hoc NVARCHAR(150) NOT NULL,
    loai_bai_hoc VARCHAR(30) NOT NULL, -- ly_thuyet/thuc_hanh
    thu_tu INT NOT NULL,
    noi_dung NVARCHAR(MAX) NULL,
    thoi_luong_phut INT NULL,
    CONSTRAINT fk_bai_hoc_giao_trinh FOREIGN KEY (giao_trinh_id) REFERENCES giao_trinh(id)
);
```

API:

- CRUD `curriculums`.
- CRUD `lessons`.
- CRUD `topics` thay vì chỉ list.

### 5.6. Ngân hàng câu hỏi chưa hiện tốt trong dashboard

BE có `/api/v1/admin/questions` và `/api/v1/questions/with-answers`. Dashboard hiện gọi admin questions nhưng chỉ hiển thị 80 dòng, thiếu đáp án/ảnh/filter/pagination.

Đề xuất:

- Admin dashboard nên gọi endpoint DTO chuẩn: `/api/v1/admin/questions?includeAnswers=true&page=1&pageSize=50&topicId=&status=`.
- Không trả raw EF entity trực tiếp.
- Thêm API import/export câu hỏi từ Excel/PDF nếu cần.
- FE cần modal CRUD câu hỏi + đáp án, upload ảnh, chọn topic, chọn câu điểm liệt.

DB đã đủ cơ bản; chỉ cần bổ sung nếu chưa có:

- `cau_hoi.ma_cau_hoi` unique để quản lý tốt hơn.
- `cau_hoi.hang_bang` để phân biệt A1/A/B1...
- `cau_hoi.created_by`, `updated_by`, `approved_by`.

### 5.7. Đề thi sát hạch chưa thấy dữ liệu từ API

BE có `de_thi`, `de_thi_cau_hoi`, admin `/api/v1/admin/exams`, mock `/api/v1/mock-exams`. SQL `11_generate_mock_exams_a1_a.sql` sinh đề thi thử, nhưng dashboard có thể rỗng vì:

- `loai_de_thi` không khớp filter.
- `trang_thai` không phải `hoat_dong`/published.
- Dashboard không phân biệt đề thi thử và đề sát hạch thật.
- Seed thiếu `ky_thi` tương ứng hoặc FK không đúng.

Đề xuất chuẩn hóa `de_thi.loai_de_thi`:

- `thi_thu`
- `sat_hach`
- `on_tap`

API:

- `/api/v1/admin/exam-papers?type=sat_hach`
- `/api/v1/admin/exam-papers/{id}/questions`
- `/api/v1/admin/exam-papers/{id}/publish`
- `/api/v1/admin/exam-papers/{id}/clone`

FE cần tab riêng: Đề thi thử / Đề sát hạch / Bản nháp.

### 5.8. Quản lý thi cử chưa thấy dữ liệu API

Hiện có `ky_thi`, `ca_thi`, `dang_ky_du_thi`, `bai_thi`. Admin chỉ list `exam-periods` và `exam-registrations`, chưa có CRUD kỳ thi/ca thi.

Đề xuất API:

- CRUD `exam-periods`.
- CRUD `exam-sessions`/`ca-thi`.
- Assign exam paper to session.
- Approve/reject exam registrations.
- Generate exam attempts for approved students.
- Attendance/check-in exam.

DB nên thêm vào `ca_thi`:

- `de_thi_id` nullable hoặc bảng mapping `ca_thi_de_thi` nếu một ca có nhiều đề.
- `giam_thi_id` trỏ giáo viên/người dùng.
- `so_luong_toi_da`.

### 5.9. Kết quả thi chưa thấy dữ liệu API

BE có `/api/v1/admin/exam-results`, nhưng dữ liệu có thể rỗng nếu mock exam chỉ lưu `bai_thi` mà không tạo `exam_results`.

Đề xuất:

- Khi submit bài thi thật: ghi `bai_thi`, `chi_tiet_bai_thi`, sau đó upsert `exam_results`.
- Với thi thử: có thể chỉ ghi `bai_thi`, nhưng dashboard cần endpoint riêng `mock-exam-results` hoặc include `bai_thi` type.
- Với thi sát hạch: bắt buộc ghi `exam_results`, có xác nhận bởi admin/giáo viên.

API cần:

- `GET /api/v1/admin/exam-results?type=&studentId=&periodId=&classId=`
- `PATCH /api/v1/admin/exam-results/{id}/confirm`
- `POST /api/v1/admin/exam-results/{id}/certificate`

### 5.10. Vai trò giáo viên và quyền thao tác

Yêu cầu giáo viên rất rộng: CRUD khóa/lớp/lịch/đề/câu hỏi/đáp án, xem học viên theo lớp, quản lý phiếu thu, duyệt học viên vào lớp sau kiểm tra học phí.

Đề xuất phân quyền không nên chỉ dùng role `giao_vien`, mà dùng permission chi tiết:

- `courses.read/create/update/delete`
- `classes.read/create/update/delete`
- `schedules.read/create/update/delete`
- `questions.read/create/update/delete/approve`
- `exam_papers.read/create/update/delete/publish`
- `exam_sessions.read/create/update/delete`
- `students.read_by_class`
- `receipts.read/create/confirm/cancel`
- `enrollments.approve_to_class`

Quan trọng: giáo viên chỉ nên thao tác trong phạm vi được phân công, trừ giáo viên quản lý/trưởng bộ môn.

Cần bảng scope:

```sql
CREATE TABLE giao_vien_lop_hoc (
    id BIGINT IDENTITY PRIMARY KEY,
    giao_vien_id BIGINT NOT NULL,
    lop_hoc_id BIGINT NOT NULL,
    vai_tro_trong_lop VARCHAR(30) NOT NULL DEFAULT 'giang_vien',
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT uq_gvlh UNIQUE(giao_vien_id, lop_hoc_id)
);
```

Flow duyệt học viên vào lớp nên là:

1. Học viên đăng ký khóa học.
2. Hệ thống tạo hoặc chờ tạo phiếu thu học phí.
3. Học viên thanh toán.
4. Phiếu thu được xác nhận `da_xac_nhan`.
5. Giáo viên/admin chọn lớp phù hợp.
6. API kiểm tra đã đóng đủ học phí của đăng ký đó.
7. Nếu đủ, tạo `lop_hoc_hoc_vien`.
8. Sinh thông báo cho học viên và giáo viên.
9. Ghi audit log.

## 6. Các bảng nên tạo/cập nhật ưu tiên

### Ưu tiên P0 - bắt buộc để đúng luồng

1. Tạo `giao_vien`.
2. Thêm/chuẩn hóa FK giáo viên trong `lop_hoc`, `diem_danh`, `buoi_hoc`.
3. Thêm `loai_buoi` vào `buoi_hoc`.
4. Thêm `thong_bao`.
5. Liên kết phiếu thu với đăng ký khóa học:
   - thêm `dang_ky_khoa_hoc_id` vào `phieu_thu` hoặc `chi_tiet_phieu_thu`.
6. Thêm kiểm tra thanh toán khi duyệt vào lớp.

### Ưu tiên P1 - hoàn thiện quản trị đào tạo/thi

1. `giao_trinh`, `bai_hoc`.
2. CRUD `chu_de_cau_hoi`.
3. Bổ sung `cau_hoi.ma_cau_hoi`, `hang_bang`, audit fields.
4. CRUD kỳ thi/ca thi.
5. Mapping `ca_thi` với `de_thi`.
6. Upsert `exam_results` sau submit.

### Ưu tiên P2 - mở rộng vận hành

1. `phuong_tien`.
2. `san_tap`.
3. `lich_xe_tap`.
4. Notification real-time hoặc polling.
5. Import/export dữ liệu.
6. Báo cáo doanh thu/đậu rớt/tiến độ lớp.

## 7. Roadmap source code đề xuất

### Phase 1 - Database migration và model

- Tạo SQL script `15_add_teacher_notifications_schedule_type.sql`.
- Scaffold/update EF models.
- Update `ApplicationDbContext` DbSet/config.
- Backfill dữ liệu giáo viên từ role hiện tại.
- Backfill `buoi_hoc.loai_buoi` từ `ten_buoi`/`noi_dung`: nếu chứa “thực hành” thì `thuc_hanh`, còn lại `ly_thuyet`.

### Phase 2 - Backend API sạch hơn

- Tách `AdminController` thành:
  - `AdminUsersController`
  - `AdminStudentsController`
  - `AdminTeachersController`
  - `AdminCoursesController`
  - `AdminClassesController`
  - `AdminSchedulesController`
  - `AdminQuestionBankController`
  - `AdminExamPapersController`
  - `AdminExamManagementController`
  - `AdminPaymentsController`
  - `AdminNotificationsController`
- Dùng service layer + DTO + validators.
- Thêm authorization policies theo permission.
- Không trả raw EF entity cho admin UI.

### Phase 3 - Frontend admin CRUD

- Mỗi module có:
  - list/search/filter/pagination,
  - create modal,
  - edit modal,
  - delete/archive,
  - validate form,
  - toast/error state.
- Tách JS theo module thay vì một `admin-unified.js` quá lớn:
  - `admin-api.js`
  - `admin-training.js`
  - `admin-exams.js`
  - `admin-payments.js`
  - `admin-notifications.js`

### Phase 4 - Teacher portal

- Thêm route MVC `/Teacher` hoặc area `Teacher`.
- Teacher chỉ xem/quản lý dữ liệu thuộc scope.
- Chức năng chính:
  - Lớp của tôi.
  - Học viên trong lớp.
  - Lịch dạy.
  - Soạn đề/câu hỏi.
  - Phiếu thu cần xác nhận.
  - Duyệt học viên vào lớp sau thanh toán.

## 8. Đánh giá chức năng đã đầy đủ chưa

Chưa đầy đủ.

Mức hiện tại:

- Backend schema: khoảng 65-70% nền nghiệp vụ.
- Backend CRUD admin: khoảng 45-55%.
- Frontend admin CRUD: khoảng 25-35%.
- Luồng học phí -> duyệt lớp: chưa chuẩn.
- Luồng giáo viên: chưa chuẩn.
- Luồng thi sát hạch thật: mới có bảng lõi, chưa đủ quản trị trọn vẹn.
- Dashboard cảnh báo: chưa có nền dữ liệu thông báo.

## 9. Kết luận kiến trúc

Hướng tốt nhất là không vá tiếp bằng cách thêm nhiều query trực tiếp vào một controller lớn. Nên nâng cấp theo hướng:

1. Chuẩn hóa schema nghiệp vụ trước: giáo viên, thông báo, loại lịch, liên kết học phí.
2. Chuẩn hóa API bằng DTO/service/validator/policy.
3. Hoàn thiện FE CRUD theo module.
4. Tách teacher portal hoặc teacher mode riêng trong admin.
5. Thêm dữ liệu seed có kiểm soát và migration script tuần tự.

Nếu làm theo thứ tự này, hệ thống sẽ tránh lỗi dữ liệu về sau, đặc biệt các lỗi: không có tên giáo viên, mã lớp/khóa lệch chuẩn, duyệt học viên chưa đóng học phí, dashboard rỗng dữ liệu thi, và giáo viên thao tác vượt phạm vi được phân công.
