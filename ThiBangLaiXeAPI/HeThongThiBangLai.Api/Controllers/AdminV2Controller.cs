using System.Data;
using System.Security.Claims;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Controllers;

[ApiController]
[Route("api/v1/admin/v2")]
[Authorize]
[Produces("application/json")]
public sealed class AdminV2Controller : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminV2Controller(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers(CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "teachers.read", "classes.read", "courses.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT
                gv.id,
                gv.nguoi_dung_id,
                gv.ma_giao_vien,
                gv.ho_ten,
                gv.ngay_sinh,
                gv.gioi_tinh,
                gv.cccd,
                gv.so_gplx,
                gv.hang_gplx,
                gv.chuyen_mon,
                gv.kinh_nghiem_nam,
                gv.trang_thai,
                gv.created_at,
                nd.ten_dang_nhap,
                nd.email,
                nd.so_dien_thoai,
                COUNT(DISTINCT gvlh.lop_hoc_id) AS class_count
            FROM dbo.giao_vien AS gv
            INNER JOIN dbo.nguoi_dung AS nd ON nd.id = gv.nguoi_dung_id
            LEFT JOIN dbo.giao_vien_lop_hoc AS gvlh ON gvlh.giao_vien_id = gv.id
            GROUP BY gv.id, gv.nguoi_dung_id, gv.ma_giao_vien, gv.ho_ten, gv.ngay_sinh, gv.gioi_tinh,
                     gv.cccd, gv.so_gplx, gv.hang_gplx, gv.chuyen_mon, gv.kinh_nghiem_nam,
                     gv.trang_thai, gv.created_at, nd.ten_dang_nhap, nd.email, nd.so_dien_thoai
            ORDER BY gv.id DESC;
            """, cancellationToken);
        return Ok(ApiResponseFactory.Success(rows, "Lấy danh sách giáo viên thành công"));
    }

    [HttpPost("teachers")]
    public async Task<IActionResult> CreateTeacher([FromBody] UpsertTeacherV2Request request, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "teachers.create", "classes.create")) return Forbid();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.giao_vien
            (nguoi_dung_id, ma_giao_vien, ho_ten, ngay_sinh, gioi_tinh, cccd, so_gplx, hang_gplx, chuyen_mon, kinh_nghiem_nam, trang_thai)
            OUTPUT INSERTED.id
            VALUES
            (@nguoi_dung_id, @ma_giao_vien, @ho_ten, @ngay_sinh, @gioi_tinh, @cccd, @so_gplx, @hang_gplx, @chuyen_mon, @kinh_nghiem_nam, @trang_thai);
            """;
        Add(command, "@nguoi_dung_id", request.NguoiDungId);
        Add(command, "@ma_giao_vien", request.MaGiaoVien);
        Add(command, "@ho_ten", request.HoTen);
        Add(command, "@ngay_sinh", request.NgaySinh);
        Add(command, "@gioi_tinh", request.GioiTinh);
        Add(command, "@cccd", request.Cccd);
        Add(command, "@so_gplx", request.SoGplx);
        Add(command, "@hang_gplx", request.HangGplx);
        Add(command, "@chuyen_mon", request.ChuyenMon);
        Add(command, "@kinh_nghiem_nam", request.KinhNghiemNam);
        Add(command, "@trang_thai", request.TrangThai ?? "hoat_dong");
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        await AddNotificationAsync("teacher_created", "info", "Tạo hồ sơ giáo viên", $"Đã tạo giáo viên {request.HoTen}.", "admin", "giao_vien", id, cancellationToken);
        return Ok(ApiResponseFactory.Created(new { id }, "Tạo giáo viên thành công"));
    }

    [HttpPut("teachers/{id:long}")]
    public async Task<IActionResult> UpdateTeacher(long id, [FromBody] UpsertTeacherV2Request request, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "teachers.update", "classes.update")) return Forbid();
        var affected = await ExecuteAsync("""
            UPDATE dbo.giao_vien
            SET ma_giao_vien = @ma_giao_vien,
                ho_ten = @ho_ten,
                ngay_sinh = @ngay_sinh,
                gioi_tinh = @gioi_tinh,
                cccd = @cccd,
                so_gplx = @so_gplx,
                hang_gplx = @hang_gplx,
                chuyen_mon = @chuyen_mon,
                kinh_nghiem_nam = @kinh_nghiem_nam,
                trang_thai = @trang_thai,
                updated_at = SYSUTCDATETIME()
            WHERE id = @id;
            """, cancellationToken,
            ("@id", id), ("@ma_giao_vien", request.MaGiaoVien), ("@ho_ten", request.HoTen),
            ("@ngay_sinh", request.NgaySinh), ("@gioi_tinh", request.GioiTinh), ("@cccd", request.Cccd),
            ("@so_gplx", request.SoGplx), ("@hang_gplx", request.HangGplx), ("@chuyen_mon", request.ChuyenMon),
            ("@kinh_nghiem_nam", request.KinhNghiemNam), ("@trang_thai", request.TrangThai ?? "hoat_dong"));
        return affected == 0 ? NotFound(ApiResponseFactory.Fail("Không tìm thấy giáo viên")) : Ok(ApiResponseFactory.Success(new { id }, "Cập nhật giáo viên thành công"));
    }

    [HttpDelete("teachers/{id:long}")]
    public async Task<IActionResult> ArchiveTeacher(long id, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "teachers.delete", "classes.delete")) return Forbid();
        var affected = await ExecuteAsync("UPDATE dbo.giao_vien SET trang_thai = 'ngung_day', updated_at = SYSUTCDATETIME() WHERE id = @id;", cancellationToken, ("@id", id));
        return affected == 0 ? NotFound(ApiResponseFactory.Fail("Không tìm thấy giáo viên")) : Ok(ApiResponseFactory.Success(new { id }, "Ngưng hoạt động giáo viên thành công"));
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] long? classId, [FromQuery] string? type, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "schedules.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT bh.id, bh.lop_hoc_id, lh.ma_lop, lh.ten_lop, bh.ten_buoi, bh.ngay_hoc, bh.gio_bat_dau, bh.gio_ket_thuc,
                   bh.loai_buoi, bh.dia_diem, bh.phong_hoc, bh.noi_dung, bh.giao_vien_id, gv.ho_ten AS teacher_name
            FROM dbo.buoi_hoc AS bh
            INNER JOIN dbo.lop_hoc AS lh ON lh.id = bh.lop_hoc_id
            LEFT JOIN dbo.giao_vien AS gv ON gv.id = bh.giao_vien_id
            WHERE (@classId IS NULL OR bh.lop_hoc_id = @classId)
              AND (@type IS NULL OR bh.loai_buoi = @type)
            ORDER BY bh.ngay_hoc, bh.gio_bat_dau, bh.id;
            """, cancellationToken, ("@classId", classId), ("@type", type));
        return Ok(ApiResponseFactory.Success(rows, "Lấy lịch học thành công"));
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] UpsertScheduleV2Request request, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "schedules.create")) return Forbid();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.buoi_hoc
            (lop_hoc_id, ten_buoi, ngay_hoc, gio_bat_dau, gio_ket_thuc, noi_dung, phong_hoc, loai_buoi, dia_diem, giao_vien_id)
            OUTPUT INSERTED.id
            VALUES (@classId, @name, @date, @start, @end, @content, @room, @type, @location, @teacherId);
            """;
        Add(command, "@classId", request.ClassId); Add(command, "@name", request.Name); Add(command, "@date", request.StudyDate);
        Add(command, "@start", request.StartTime); Add(command, "@end", request.EndTime); Add(command, "@content", request.Content);
        Add(command, "@room", request.Room); Add(command, "@type", request.Type ?? "ly_thuyet"); Add(command, "@location", request.Location ?? request.Room);
        Add(command, "@teacherId", request.TeacherId);
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        return Ok(ApiResponseFactory.Created(new { id }, "Tạo lịch học thành công"));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] bool? unread, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "notifications.read", "courses.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT TOP (100) id, loai, muc_do, tieu_de, noi_dung, doi_tuong, nguoi_nhan_id, entity_type, entity_id, da_doc, trang_thai, created_at, read_at
            FROM dbo.thong_bao
            WHERE (@unread IS NULL OR da_doc = CASE WHEN @unread = 1 THEN 0 ELSE da_doc END)
            ORDER BY created_at DESC, id DESC;
            """, cancellationToken, ("@unread", unread));
        return Ok(ApiResponseFactory.Success(rows, "Lấy thông báo thành công"));
    }

    [HttpPatch("notifications/{id:long}/read")]
    public async Task<IActionResult> MarkNotificationRead(long id, CancellationToken cancellationToken)
    {
        var affected = await ExecuteAsync("UPDATE dbo.thong_bao SET da_doc = 1, read_at = SYSUTCDATETIME() WHERE id = @id;", cancellationToken, ("@id", id));
        return affected == 0 ? NotFound(ApiResponseFactory.Fail("Không tìm thấy thông báo")) : Ok(ApiResponseFactory.Success(new { id }, "Đã đọc thông báo"));
    }

    [HttpGet("curriculums")]
    public async Task<IActionResult> GetCurriculums(CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "curriculums.read", "questions.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT gt.id, gt.ma_giao_trinh, gt.ten_giao_trinh, gt.hang_bang, gt.mo_ta, gt.trang_thai, COUNT(bh.id) AS lesson_count
            FROM dbo.giao_trinh AS gt
            LEFT JOIN dbo.bai_hoc AS bh ON bh.giao_trinh_id = gt.id
            GROUP BY gt.id, gt.ma_giao_trinh, gt.ten_giao_trinh, gt.hang_bang, gt.mo_ta, gt.trang_thai
            ORDER BY gt.id DESC;
            """, cancellationToken);
        return Ok(ApiResponseFactory.Success(rows, "Lấy giáo trình thành công"));
    }

    [HttpPost("curriculums")]
    public async Task<IActionResult> CreateCurriculum([FromBody] UpsertCurriculumRequest request, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "curriculums.create", "questions.create")) return Forbid();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.giao_trinh (ma_giao_trinh, ten_giao_trinh, hang_bang, mo_ta, trang_thai)
            OUTPUT INSERTED.id
            VALUES (@code, @name, @license, @description, @status);
            """;
        Add(command, "@code", request.Code); Add(command, "@name", request.Name); Add(command, "@license", request.LicenseClass);
        Add(command, "@description", request.Description); Add(command, "@status", request.Status ?? "active");
        var id = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        return Ok(ApiResponseFactory.Created(new { id }, "Tạo giáo trình thành công"));
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetLessons([FromQuery] long? curriculumId, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "curriculums.read", "questions.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT bh.id, bh.giao_trinh_id, gt.ten_giao_trinh, bh.ma_bai_hoc, bh.ten_bai_hoc, bh.loai_bai_hoc,
                   bh.thu_tu, bh.noi_dung, bh.thoi_luong_phut, bh.trang_thai
            FROM dbo.bai_hoc AS bh
            INNER JOIN dbo.giao_trinh AS gt ON gt.id = bh.giao_trinh_id
            WHERE (@curriculumId IS NULL OR bh.giao_trinh_id = @curriculumId)
            ORDER BY bh.giao_trinh_id, bh.thu_tu;
            """, cancellationToken, ("@curriculumId", curriculumId));
        return Ok(ApiResponseFactory.Success(rows, "Lấy bài học thành công"));
    }

    [HttpGet("exam-papers")]
    public async Task<IActionResult> GetExamPapers([FromQuery] string? type, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "exam_papers.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT dt.id, dt.ma_de_thi, dt.ten_de_thi, dt.ky_thi_id, kt.ten_ky_thi, dt.tong_so_cau,
                   dt.thoi_gian_lam_bai, dt.trang_thai, dt.loai_de_thi, dt.hang_bang, dt.is_public,
                   dt.ngay_tao, dt.published_at, nd.ten_dang_nhap AS created_by_name,
                   COUNT(dtch.id) AS question_count
            FROM dbo.de_thi AS dt
            INNER JOIN dbo.ky_thi AS kt ON kt.id = dt.ky_thi_id
            LEFT JOIN dbo.nguoi_dung AS nd ON nd.id = dt.nguoi_tao_id
            LEFT JOIN dbo.de_thi_cau_hoi AS dtch ON dtch.de_thi_id = dt.id
            WHERE (@type IS NULL OR dt.loai_de_thi = @type)
            GROUP BY dt.id, dt.ma_de_thi, dt.ten_de_thi, dt.ky_thi_id, kt.ten_ky_thi, dt.tong_so_cau,
                     dt.thoi_gian_lam_bai, dt.trang_thai, dt.loai_de_thi, dt.hang_bang, dt.is_public,
                     dt.ngay_tao, dt.published_at, nd.ten_dang_nhap
            ORDER BY dt.id DESC;
            """, cancellationToken, ("@type", type));
        return Ok(ApiResponseFactory.Success(rows, "Lấy đề thi thành công"));
    }

    [HttpPatch("exam-papers/{id:long}/publish")]
    public async Task<IActionResult> PublishExamPaper(long id, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "exam_papers.publish")) return Forbid();
        var affected = await ExecuteAsync("""
            UPDATE dbo.de_thi
            SET trang_thai = 'hoat_dong', is_public = 1, published_at = SYSUTCDATETIME(), published_by = @userId
            WHERE id = @id;
            """, cancellationToken, ("@id", id), ("@userId", GetCurrentUserId()));
        return affected == 0 ? NotFound(ApiResponseFactory.Fail("Không tìm thấy đề thi")) : Ok(ApiResponseFactory.Success(new { id }, "Công bố đề thi thành công"));
    }

    [HttpGet("exam-sessions")]
    public async Task<IActionResult> GetExamSessions(CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "exam_sessions.read")) return Forbid();
        var rows = await QueryAsync("""
            SELECT ct.id, ct.ky_thi_id, kt.ma_ky_thi, kt.ten_ky_thi, kt.ngay_thi, ct.ma_ca_thi, ct.ten_ca_thi,
                   ct.gio_bat_dau, ct.gio_ket_thuc, ct.phong_thi, ct.so_luong_toi_da, ct.trang_thai,
                   ct.giam_thi_id, gv.ho_ten AS examiner_name, COUNT(dk.id) AS registration_count
            FROM dbo.ca_thi AS ct
            INNER JOIN dbo.ky_thi AS kt ON kt.id = ct.ky_thi_id
            LEFT JOIN dbo.giao_vien AS gv ON gv.id = ct.giam_thi_id
            LEFT JOIN dbo.dang_ky_du_thi AS dk ON dk.ca_thi_id = ct.id
            GROUP BY ct.id, ct.ky_thi_id, kt.ma_ky_thi, kt.ten_ky_thi, kt.ngay_thi, ct.ma_ca_thi, ct.ten_ca_thi,
                     ct.gio_bat_dau, ct.gio_ket_thuc, ct.phong_thi, ct.so_luong_toi_da, ct.trang_thai,
                     ct.giam_thi_id, gv.ho_ten
            ORDER BY kt.ngay_thi DESC, ct.gio_bat_dau;
            """, cancellationToken);
        return Ok(ApiResponseFactory.Success(rows, "Lấy ca thi thành công"));
    }

    [HttpPost("classes/{classId:long}/students/{studentId:long}/approve-paid")]
    public async Task<IActionResult> ApprovePaidStudentToClass(long classId, long studentId, CancellationToken cancellationToken)
    {
        if (!await HasAnyPermissionAsync(cancellationToken, "enrollments.approve_to_class")) return Forbid();
        var data = await QueryAsync("""
            SELECT TOP (1) dkkh.id AS registration_id
            FROM dbo.dang_ky_khoa_hoc AS dkkh
            INNER JOIN dbo.lop_hoc AS lh ON lh.khoa_hoc_id = dkkh.khoa_hoc_id
            INNER JOIN dbo.phieu_thu AS pt ON pt.dang_ky_khoa_hoc_id = dkkh.id AND pt.trang_thai = 'da_xac_nhan'
            WHERE lh.id = @classId AND dkkh.hoc_vien_id = @studentId
            ORDER BY pt.ngay_xac_nhan DESC, pt.id DESC;
            """, cancellationToken, ("@classId", classId), ("@studentId", studentId));
        if (data.Count == 0)
        {
            await AddNotificationAsync("enrollment_blocked", "warning", "Không thể duyệt học viên vào lớp", "Học viên chưa có phiếu thu học phí đã xác nhận cho khóa học này.", "admin", "lop_hoc", classId, cancellationToken);
            return BadRequest(ApiResponseFactory.Fail("Học viên chưa hoàn thành học phí hoặc chưa có phiếu thu đã xác nhận."));
        }

        await ExecuteAsync("""
            IF NOT EXISTS (SELECT 1 FROM dbo.lop_hoc_hoc_vien WHERE lop_hoc_id = @classId AND hoc_vien_id = @studentId)
                INSERT INTO dbo.lop_hoc_hoc_vien (lop_hoc_id, hoc_vien_id, ngay_vao_lop, trang_thai)
                VALUES (@classId, @studentId, CONVERT(date, SYSUTCDATETIME()), 'dang_hoc');
            ELSE
                UPDATE dbo.lop_hoc_hoc_vien
                SET trang_thai = 'dang_hoc', ngay_vao_lop = ISNULL(ngay_vao_lop, CONVERT(date, SYSUTCDATETIME()))
                WHERE lop_hoc_id = @classId AND hoc_vien_id = @studentId;
            """, cancellationToken, ("@classId", classId), ("@studentId", studentId));
        await AddNotificationAsync("enrollment_approved", "info", "Duyệt học viên vào lớp", "Học viên đã được duyệt vào lớp sau khi xác nhận học phí.", "admin", "lop_hoc", classId, cancellationToken);
        return Ok(ApiResponseFactory.Success(new { classId, studentId }, "Duyệt học viên vào lớp thành công"));
    }

    private long? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(value, out var id) ? id : null;
    }

    private async Task<bool> HasAnyPermissionAsync(CancellationToken cancellationToken, params string[] permissions)
    {
        var roleClaims = User.FindAll(ClaimTypes.Role).Select(item => item.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roleClaims.Contains("ADMIN") || roleClaims.Contains("admin")) return true;

        var userId = GetCurrentUserId();
        if (userId is null) return false;

        var placeholders = permissions.Select((_, index) => $"@p{index}").ToArray();
        var sql = $"""
            SELECT COUNT(1)
            FROM dbo.nguoi_dung_vai_tro AS ndvt
            INNER JOIN dbo.vai_tro AS vt ON vt.id = ndvt.vai_tro_id
            LEFT JOIN dbo.vai_tro_quyen_han AS vtqh ON vtqh.vai_tro_id = vt.id
            LEFT JOIN dbo.quyen_han AS qh ON qh.id = vtqh.quyen_han_id
            WHERE ndvt.nguoi_dung_id = @userId
              AND (vt.ma_vai_tro IN ('admin', 'ADMIN') OR qh.ma_quyen IN ({string.Join(",", placeholders)}));
            """;
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        Add(command, "@userId", userId);
        for (var i = 0; i < permissions.Length; i++) Add(command, $"@p{i}", permissions[i]);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    private async Task AddNotificationAsync(string type, string severity, string title, string content, string target, string entityType, long? entityId, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync("thong_bao", cancellationToken)) return;
        await ExecuteAsync("""
            INSERT INTO dbo.thong_bao (loai, muc_do, tieu_de, noi_dung, doi_tuong, entity_type, entity_id)
            VALUES (@type, @severity, @title, @content, @target, @entityType, @entityId);
            """, cancellationToken, ("@type", type), ("@severity", severity), ("@title", title), ("@content", content), ("@target", target), ("@entityType", entityType), ("@entityId", entityId));
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sys.tables WHERE name = @tableName AND schema_id = SCHEMA_ID('dbo');";
        Add(command, "@tableName", tableName);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    private async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Value);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }

    private async Task<int> ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters) Add(command, parameter.Name, parameter.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_dbContext.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void Add(SqlCommand command, string name, object? value)
    {
        command.Parameters.Add(new SqlParameter(name, value ?? DBNull.Value));
    }
}

public sealed record UpsertTeacherV2Request(
    long NguoiDungId,
    string MaGiaoVien,
    string HoTen,
    DateOnly? NgaySinh,
    string? GioiTinh,
    string? Cccd,
    string? SoGplx,
    string? HangGplx,
    string? ChuyenMon,
    int? KinhNghiemNam,
    string? TrangThai);

public sealed record UpsertScheduleV2Request(
    long ClassId,
    string Name,
    DateOnly StudyDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Content,
    string? Room,
    string? Type,
    string? Location,
    long? TeacherId);

public sealed record UpsertCurriculumRequest(
    string Code,
    string Name,
    string? LicenseClass,
    string? Description,
    string? Status);
