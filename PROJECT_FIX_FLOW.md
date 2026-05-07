# Quy trình đọc source và fix lỗi chuẩn dự án

Tài liệu này ghi lại luồng phân tích đã dùng để xử lý lỗi Admin không tải được ngân hàng câu hỏi. Khi cần sửa lỗi hoặc phát triển chức năng mới, ưu tiên đi theo thứ tự: **Database → Backend → Frontend**.

## 1. Nguyên tắc tổng quát

- Không sửa giao diện hoặc JavaScript theo cảm tính khi chưa xác minh dữ liệu và API.
- Luôn bắt đầu từ bảng dữ liệu liên quan để hiểu schema, khóa chính, khóa ngoại, trạng thái, dữ liệu thật.
- Sau đó kiểm tra backend: model, `DbContext`, controller, service/repository, DTO/response.
- Cuối cùng mới kiểm tra frontend: controller MVC, service proxy, Razor View/partial, script thuộc luồng MVC.
- Không đặt business logic vào `webthibanglai/wwwroot` nếu thư mục đó chỉ là template UI tham khảo.

## 2. Luồng chuẩn khi fix lỗi

### Bước 1: Database

Cần xác định:

- Bảng nào đang phục vụ màn hình hoặc chức năng lỗi.
- Bảng có dữ liệu thật hay không.
- Các cột bắt buộc, kiểu dữ liệu, giá trị mặc định.
- Khóa ngoại và các bảng liên quan.
- Trạng thái dữ liệu đang dùng, ví dụ `hoat_dong`, `dang_mo`, `da_duyet`, `approved`.

Ví dụ với ngân hàng câu hỏi:

- Bảng chính: `cau_hoi`.
- Bảng liên quan: `chu_de_cau_hoi`, `dap_an`, `de_thi_cau_hoi`, `chi_tiet_bai_thi`, `phien_on_tap_cau_hoi`.
- Cần kiểm tra dữ liệu thật trong `cau_hoi` trước khi kết luận lỗi frontend.

### Bước 2: Backend model và mapping

Cần đọc các file liên quan:

- Model entity, ví dụ [`cau_hoi`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Models/cau_hoi.cs:6).
- Model liên quan, ví dụ [`dap_an`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Models/dap_an.cs:6).
- Mapping trong [`ApplicationDbContext`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Data/ApplicationDbContext.cs:183).

Các điểm phải kiểm tra:

- Entity có navigation property hai chiều hay không.
- Có nguy cơ JSON cycle khi trả trực tiếp EF entity hay không.
- `DbContext` mapping có khớp schema database không.
- Cột có tồn tại trong database nhưng thiếu trong model không, hoặc ngược lại.

Bài học từ lỗi `cau_hoi`:

- [`cau_hoi`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Models/cau_hoi.cs:6) có collection `dap_ans`.
- [`dap_an`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Models/dap_an.cs:18) có navigation ngược về `cau_hoi`.
- Nếu controller trả entity trực tiếp bằng `Include(item => item.dap_ans)`, serializer có thể gặp vòng lặp `cau_hoi -> dap_ans -> cau_hoi` và backend trả lỗi chung.

### Bước 3: Backend Controller / Service / Repository

Cần xác định endpoint frontend đang gọi thật sự là endpoint nào.

Ví dụ Admin câu hỏi gọi:

- [`AdminController.GetQuestions()`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/Controllers/AdminController.cs:106)
- Route: `GET /api/v1/admin/questions`

Checklist backend:

- Route đúng chưa.
- Có `[Authorize]` hoặc permission nào làm endpoint bị chặn không.
- Query có `Include` tạo object graph lớn hoặc cycle không.
- Controller có trả EF entity trực tiếp không.
- Response có theo envelope chuẩn `ApiResponseFactory.Success(...)` không.
- Create/update/delete có trả entity gây cycle không.

Khuyến nghị:

- Với API trả dữ liệu cho Admin UI, ưu tiên projection sang DTO/response record.
- Không trả trực tiếp EF entity có navigation property.
- Nếu cần nested data, chỉ select các field cần thiết.

Ví dụ pattern đúng:

```csharp
var data = await _dbContext.cau_hois
    .AsNoTracking()
    .OrderBy(item => item.id)
    .Select(item => new AdminQuestionResponse(
        item.id,
        item.chu_de_id,
        item.noi_dung,
        item.giai_thich_dap_an,
        item.loai_cau_hoi,
        item.muc_do,
        item.la_cau_diem_liet,
        item.trang_thai,
        item.dap_ans
            .OrderBy(answer => answer.thu_tu)
            .Select(answer => new AdminAnswerResponse(
                answer.id,
                answer.cau_hoi_id,
                answer.noi_dung,
                answer.la_dap_an_dung,
                answer.thu_tu))
            .ToList()))
    .ToListAsync();
```

### Bước 4: Frontend MVC flow

Sau khi backend đã rõ, mới kiểm tra frontend.

Cần đọc theo thứ tự:

1. MVC Controller, ví dụ [`webthibanglai/Controllers/AdminController.cs`](webthibanglai/Controllers/AdminController.cs:6).
2. Service gọi API/proxy, ví dụ [`AdminApiService`](webthibanglai/Services/AdminApiService.cs:13).
3. Razor page chính, ví dụ [`webthibanglai/Views/Admin/Index.cshtml`](webthibanglai/Views/Admin/Index.cshtml:1).
4. Partial views, ví dụ:
   - [`_AdminExamSections.cshtml`](webthibanglai/Views/Admin/_AdminExamSections.cshtml:56)
   - [`_AdminTrainingSections.cshtml`](webthibanglai/Views/Admin/_AdminTrainingSections.cshtml:70)
5. Script đúng luồng MVC/Razor, ví dụ [`_AdminUnifiedScripts.cshtml`](webthibanglai/Views/Admin/_AdminUnifiedScripts.cshtml:1).

Checklist frontend:

- Browser gọi đúng MVC proxy hoặc API endpoint không.
- Session token có tồn tại không.
- Proxy có preserve/hiển thị lỗi backend rõ ràng không.
- JSON response shape có đúng với code render không.
- Naming policy là snake_case, camelCase hay PascalCase.
- UI không nuốt lỗi bằng cách fallback thành list rỗng.

## 3. Quy tắc đặt logic frontend

- `webthibanglai/wwwroot` chỉ dùng cho static assets/template tham khảo nếu project đã xác định như vậy.
- Business/Admin UI logic nên đặt theo MVC/Razor flow:
  - Controller MVC.
  - Service gọi backend.
  - Razor view/partial.
  - Razor-owned script partial nếu cần script riêng cho trang.

Không nên sửa logic nghiệp vụ trong file template tĩnh như `wwwroot/admin/admin-unified.js` nếu file đó chỉ là mẫu UI.

## 4. Quy trình kiểm chứng sau khi sửa

### Backend

Nếu app backend đang chạy và lock file output, dùng build output riêng:

```bash
dotnet build ThiBangLaiXeAPI/HeThongThiBangLai.Api/HeThongThiBangLai.Api.csproj --no-restore -p:UseAppHost=false -p:OutDir=c:\moto_license_project\ThiBangLaiXe\VerifyBackendOutput\
```

Nếu tạo output phụ trong project, cần exclude các folder verify khỏi project item discovery trong [`HeThongThiBangLai.Api.csproj`](ThiBangLaiXeAPI/HeThongThiBangLai.Api/HeThongThiBangLai.Api.csproj:10).

### Frontend

Nếu app frontend đang chạy và lock DLL output, dùng output riêng:

```bash
dotnet build webthibanglai/webthibanglai.csproj --no-restore -p:OutDir=artifacts\frontend-verify\
```

## 5. Dấu hiệu lỗi thường gặp và hướng xử lý

### UI báo “Chưa có dữ liệu từ API”

Không kết luận ngay là database rỗng. Kiểm tra:

- Database có dữ liệu không.
- Backend endpoint trả status gì.
- MVC proxy có nuốt status code không.
- Frontend có catch lỗi rồi trả `[]` không.
- Response property naming có lệch không.

### UI báo “An unexpected error occurred”

Ưu tiên kiểm tra backend:

- Controller có trả EF entity trực tiếp không.
- Entity có navigation cycle không.
- Query có Include quá sâu không.
- Response serialization có lỗi không.
- Exception middleware đang che message thật không.

### Build lỗi do file bị lock

Nếu đang chạy server, build thường có thể lỗi vì DLL hoặc EXE bị lock. Dùng output path riêng để verify compile, hoặc dừng process đang chạy.

### Build lỗi do nested output/staticwebassets

Nếu build output phụ nằm trong project folder và không được exclude, SDK có thể đưa file output cũ vào static web assets/content. Cần exclude folder verify trong `.csproj`.

## 6. Checklist bắt buộc trước khi sửa lỗi tương tự

- [ ] Xác định bảng database và dữ liệu thật.
- [ ] Đọc model entity và navigation properties.
- [ ] Đọc `ApplicationDbContext` mapping.
- [ ] Xác định endpoint backend thật sự được gọi.
- [ ] Kiểm tra controller/service/repository có trả entity trực tiếp không.
- [ ] Nếu có navigation cycle, chuyển sang DTO/projection.
- [ ] Kiểm tra MVC controller/service proxy ở frontend.
- [ ] Kiểm tra Razor view/partial và script đúng vị trí project.
- [ ] Không đặt logic vào thư mục template/static nếu project không cho phép.
- [ ] Build verify backend và frontend.
- [ ] Ghi rõ root cause, file sửa, lệnh verify.

## 7. Kết luận

Luồng sửa chuẩn của dự án là:

```text
Database schema + data
    -> Backend entity/model + DbContext mapping
    -> Backend controller/service/repository + DTO response
    -> Frontend MVC controller + service proxy
    -> Razor views/partials + page-owned scripts
    -> Build/verify
```

Đi theo luồng này giúp tránh sửa sai tầng, tránh vá tạm trên UI, và tìm đúng nguyên nhân gốc của lỗi.
