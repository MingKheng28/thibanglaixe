using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public AdminController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _dbContext.nguoi_dungs
            .AsNoTracking()
            .Include(item => item.nguoi_dung_vai_tros)
            .ThenInclude(item => item.vai_tro)
            .Select(item => new
            {
                item.id,
                item.ten_dang_nhap,
                item.email,
                item.so_dien_thoai,
                item.trang_thai,
                item.created_at,
                item.updated_at,
                roles = item.nguoi_dung_vai_tros.Select(role => new
                {
                    role.vai_tro.id,
                    role.vai_tro.ma_vai_tro,
                    role.vai_tro.ten_vai_tro
                })
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(users, "Lấy danh sách người dùng thành công"));
    }

    [HttpPatch("users/{userId:long}/status")]
    public async Task<IActionResult> UpdateUserStatus(long userId, [FromBody] UpdateStatusRequest request)
    {
        var user = await _dbContext.nguoi_dungs.FindAsync(userId);
        if (user is null)
        {
            return NotFound(ApiResponseFactory.Fail("Không tìm thấy người dùng"));
        }

        user.trang_thai = request.Status;
        user.updated_at = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(user, "Cập nhật trạng thái người dùng thành công"));
    }

    [HttpPost("users/{userId:long}/roles")]
    public async Task<IActionResult> AssignRole(long userId, [FromBody] AssignRoleRequest request)
    {
        var userExists = await _dbContext.nguoi_dungs.AnyAsync(item => item.id == userId);
        if (!userExists)
        {
            return NotFound(ApiResponseFactory.Fail("Không tìm thấy người dùng"));
        }

        var role = await _dbContext.vai_tros.FirstOrDefaultAsync(item => item.id == request.RoleId || item.ma_vai_tro == request.RoleCode);
        if (role is null)
        {
            return NotFound(ApiResponseFactory.Fail("Không tìm thấy vai trò"));
        }

        var exists = await _dbContext.nguoi_dung_vai_tros.AnyAsync(item => item.nguoi_dung_id == userId && item.vai_tro_id == role.id);
        if (!exists)
        {
            _dbContext.nguoi_dung_vai_tros.Add(new nguoi_dung_vai_tro { nguoi_dung_id = userId, vai_tro_id = role.id });
            await _dbContext.SaveChangesAsync();
        }

        return Ok(ApiResponseFactory.Success(new { userId, role.id, role.ma_vai_tro }, "Phân quyền người dùng thành công"));
    }

    [HttpDelete("users/{userId:long}/roles/{roleId:long}")]
    public async Task<IActionResult> RemoveRole(long userId, long roleId)
    {
        var userRole = await _dbContext.nguoi_dung_vai_tros.FirstOrDefaultAsync(item => item.nguoi_dung_id == userId && item.vai_tro_id == roleId);
        if (userRole is null)
        {
            return NotFound(ApiResponseFactory.Fail("Không tìm thấy phân quyền"));
        }

        _dbContext.nguoi_dung_vai_tros.Remove(userRole);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { userId, roleId }, "Gỡ vai trò người dùng thành công"));
    }

    [HttpGet("questions")]
    public async Task<IActionResult> GetQuestions()
    {
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

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách câu hỏi thành công"));
    }

    [HttpPost("questions")]
    public async Task<IActionResult> CreateQuestion([FromBody] UpsertQuestionRequest request)
    {
        var question = new cau_hoi
        {
            chu_de_id = request.TopicId,
            noi_dung = request.Content,
            giai_thich_dap_an = request.Explanation,
            loai_cau_hoi = request.QuestionType ?? "trac_nghiem",
            muc_do = request.Level,
            la_cau_diem_liet = request.IsCritical,
            trang_thai = request.Status ?? "hoat_dong"
        };
        _dbContext.cau_hois.Add(question);
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetQuestions), ApiResponseFactory.Created(MapQuestionResponse(question), "Tạo câu hỏi thành công"));
    }

    [HttpPut("questions/{id:long}")]
    public async Task<IActionResult> UpdateQuestion(long id, [FromBody] UpsertQuestionRequest request)
    {
        var question = await _dbContext.cau_hois.FindAsync(id);
        if (question is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy câu hỏi"));
        question.chu_de_id = request.TopicId;
        question.noi_dung = request.Content;
        question.giai_thich_dap_an = request.Explanation;
        question.loai_cau_hoi = request.QuestionType ?? question.loai_cau_hoi;
        question.muc_do = request.Level;
        question.la_cau_diem_liet = request.IsCritical;
        question.trang_thai = request.Status ?? question.trang_thai;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(MapQuestionResponse(question), "Cập nhật câu hỏi thành công"));
    }

    [HttpDelete("questions/{id:long}")]
    public async Task<IActionResult> DeleteQuestion(long id)
    {
        var question = await _dbContext.cau_hois.FindAsync(id);
        if (question is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy câu hỏi"));
        _dbContext.cau_hois.Remove(question);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa câu hỏi thành công"));
    }

    [HttpPost("answers")]
    public async Task<IActionResult> CreateAnswer([FromBody] UpsertAnswerRequest request)
    {
        var answer = new dap_an { cau_hoi_id = request.QuestionId, noi_dung = request.Content, la_dap_an_dung = request.IsCorrect, thu_tu = request.Order };
        _dbContext.dap_ans.Add(answer);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(answer, "Tạo đáp án thành công"));
    }

    [HttpPut("answers/{id:long}")]
    public async Task<IActionResult> UpdateAnswer(long id, [FromBody] UpsertAnswerRequest request)
    {
        var answer = await _dbContext.dap_ans.FindAsync(id);
        if (answer is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đáp án"));
        answer.cau_hoi_id = request.QuestionId;
        answer.noi_dung = request.Content;
        answer.la_dap_an_dung = request.IsCorrect;
        answer.thu_tu = request.Order;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(answer, "Cập nhật đáp án thành công"));
    }

    [HttpDelete("answers/{id:long}")]
    public async Task<IActionResult> DeleteAnswer(long id)
    {
        var answer = await _dbContext.dap_ans.FindAsync(id);
        if (answer is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đáp án"));
        _dbContext.dap_ans.Remove(answer);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa đáp án thành công"));
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams([FromQuery] string? type)
    {
        var data = await _dbContext.de_this.AsNoTracking()
            .Where(item => string.IsNullOrWhiteSpace(type) || item.loai_de_thi == type)
            .Include(item => item.de_thi_cau_hois)
            .ToListAsync();
        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách đề thi thành công"));
    }

    [HttpPost("exams")]
    public async Task<IActionResult> CreateExam([FromBody] UpsertExamRequest request)
    {
        var exam = new de_thi
        {
            ma_de_thi = request.Code,
            ten_de_thi = request.Name,
            ky_thi_id = request.ExamPeriodId,
            tong_so_cau = request.TotalQuestions,
            thoi_gian_lam_bai = request.DurationMinutes,
            trang_thai = request.Status ?? "hoat_dong",
            loai_de_thi = request.Type,
            nguoi_tao_id = GetCurrentUserId(),
            ngay_tao = DateTime.UtcNow
        };
        _dbContext.de_this.Add(exam);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(exam, "Tạo đề thi thành công"));
    }

    [HttpPut("exams/{id:long}")]
    public async Task<IActionResult> UpdateExam(long id, [FromBody] UpsertExamRequest request)
    {
        var exam = await _dbContext.de_this.FindAsync(id);
        if (exam is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đề thi"));
        exam.ma_de_thi = request.Code;
        exam.ten_de_thi = request.Name;
        exam.ky_thi_id = request.ExamPeriodId;
        exam.tong_so_cau = request.TotalQuestions;
        exam.thoi_gian_lam_bai = request.DurationMinutes;
        exam.trang_thai = request.Status ?? exam.trang_thai;
        exam.loai_de_thi = request.Type;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(exam, "Cập nhật đề thi thành công"));
    }

    [HttpPost("exams/{examId:long}/questions")]
    public async Task<IActionResult> AddQuestionToExam(long examId, [FromBody] AddExamQuestionRequest request)
    {
        var exists = await _dbContext.de_thi_cau_hois.AnyAsync(item => item.de_thi_id == examId && item.cau_hoi_id == request.QuestionId);
        if (!exists)
        {
            _dbContext.de_thi_cau_hois.Add(new de_thi_cau_hoi { de_thi_id = examId, cau_hoi_id = request.QuestionId, thu_tu_cau = request.Order });
            await _dbContext.SaveChangesAsync();
        }
        return Ok(ApiResponseFactory.Success(new { examId, request.QuestionId }, "Thêm câu hỏi vào đề thi thành công"));
    }

    [HttpDelete("exams/{id:long}")]
    public async Task<IActionResult> DeleteExam(long id)
    {
        var exam = await _dbContext.de_this.FindAsync(id);
        if (exam is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đề thi"));
        _dbContext.de_this.Remove(exam);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa đề thi thành công"));
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetCourses()
    {
        var data = await _dbContext.khoa_hocs.AsNoTracking().ToListAsync();
        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách khóa học thành công"));
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] UpsertCourseRequest request)
    {
        var course = new khoa_hoc { ma_khoa_hoc = request.Code, ten_khoa_hoc = request.Name, mo_ta = request.Description, hoc_phi = request.Fee, thoi_luong = request.Duration, trang_thai = request.Status ?? "hoat_dong" };
        _dbContext.khoa_hocs.Add(course);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(course, "Tạo khóa học thành công"));
    }

    [HttpPut("courses/{id:long}")]
    public async Task<IActionResult> UpdateCourse(long id, [FromBody] UpsertCourseRequest request)
    {
        var course = await _dbContext.khoa_hocs.FindAsync(id);
        if (course is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy khóa học"));
        course.ma_khoa_hoc = request.Code;
        course.ten_khoa_hoc = request.Name;
        course.mo_ta = request.Description;
        course.hoc_phi = request.Fee;
        course.thoi_luong = request.Duration;
        course.trang_thai = request.Status ?? course.trang_thai;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(course, "Cập nhật khóa học thành công"));
    }

    [HttpDelete("courses/{id:long}")]
    public async Task<IActionResult> DeleteCourse(long id)
    {
        var course = await _dbContext.khoa_hocs.FindAsync(id);
        if (course is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy khóa học"));
        _dbContext.khoa_hocs.Remove(course);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa khóa học thành công"));
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var data = await _dbContext.lop_hocs
            .AsNoTracking()
            .Include(item => item.khoa_hoc)
            .Include(item => item.buoi_hocs)
            .Include(item => item.lop_hoc_hoc_viens)
            .Select(item => new
            {
                item.id,
                item.khoa_hoc_id,
                course_code = item.khoa_hoc.ma_khoa_hoc,
                course_name = item.khoa_hoc.ten_khoa_hoc,
                item.ma_lop,
                item.ten_lop,
                item.giao_vien_id,
                item.ngay_bat_dau,
                item.ngay_ket_thuc,
                item.si_so_toi_da,
                current_students = item.lop_hoc_hoc_viens.Count(member => member.trang_thai == "dang_hoc"),
                schedule_count = item.buoi_hocs.Count,
                next_schedule = item.buoi_hocs
                    .Where(schedule => schedule.ngay_hoc >= today)
                    .OrderBy(schedule => schedule.ngay_hoc)
                    .ThenBy(schedule => schedule.gio_bat_dau)
                    .Select(schedule => new
                    {
                        schedule.id,
                        schedule.ten_buoi,
                        schedule.ngay_hoc,
                        schedule.gio_bat_dau,
                        schedule.gio_ket_thuc,
                        schedule.phong_hoc,
                        schedule.noi_dung
                    })
                    .FirstOrDefault(),
                item.trang_thai
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách lớp học thành công"));
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass([FromBody] UpsertClassRequest request)
    {
        var entity = new lop_hoc { khoa_hoc_id = request.CourseId, ma_lop = request.Code, ten_lop = request.Name, giao_vien_id = request.TeacherId, ngay_bat_dau = request.StartDate, ngay_ket_thuc = request.EndDate, si_so_toi_da = request.MaxStudents, trang_thai = request.Status ?? "hoat_dong" };
        _dbContext.lop_hocs.Add(entity);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(entity, "Tạo lớp học thành công"));
    }

    [HttpPut("classes/{id:long}")]
    public async Task<IActionResult> UpdateClass(long id, [FromBody] UpsertClassRequest request)
    {
        var entity = await _dbContext.lop_hocs.FindAsync(id);
        if (entity is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy lớp học"));
        entity.khoa_hoc_id = request.CourseId;
        entity.ma_lop = request.Code;
        entity.ten_lop = request.Name;
        entity.giao_vien_id = request.TeacherId;
        entity.ngay_bat_dau = request.StartDate;
        entity.ngay_ket_thuc = request.EndDate;
        entity.si_so_toi_da = request.MaxStudents;
        entity.trang_thai = request.Status ?? entity.trang_thai;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(entity, "Cập nhật lớp học thành công"));
    }

    [HttpDelete("classes/{id:long}")]
    public async Task<IActionResult> DeleteClass(long id)
    {
        var entity = await _dbContext.lop_hocs.FindAsync(id);
        if (entity is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy lớp học"));
        _dbContext.lop_hocs.Remove(entity);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa lớp học thành công"));
    }

    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules([FromQuery] long? classId)
    {
        var data = await _dbContext.buoi_hocs.AsNoTracking().Where(item => classId == null || item.lop_hoc_id == classId).ToListAsync();
        return Ok(ApiResponseFactory.Success(data, "Lấy thời khóa biểu thành công"));
    }

    [HttpPost("schedules")]
    public async Task<IActionResult> CreateSchedule([FromBody] UpsertScheduleRequest request)
    {
        var schedule = new buoi_hoc { lop_hoc_id = request.ClassId, ten_buoi = request.Name, ngay_hoc = request.StudyDate, gio_bat_dau = request.StartTime, gio_ket_thuc = request.EndTime, noi_dung = request.Content, phong_hoc = request.Room };
        _dbContext.buoi_hocs.Add(schedule);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(schedule, "Tạo thời khóa biểu thành công"));
    }

    [HttpPut("schedules/{id:long}")]
    public async Task<IActionResult> UpdateSchedule(long id, [FromBody] UpsertScheduleRequest request)
    {
        var schedule = await _dbContext.buoi_hocs.FindAsync(id);
        if (schedule is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy buổi học"));
        schedule.lop_hoc_id = request.ClassId;
        schedule.ten_buoi = request.Name;
        schedule.ngay_hoc = request.StudyDate;
        schedule.gio_bat_dau = request.StartTime;
        schedule.gio_ket_thuc = request.EndTime;
        schedule.noi_dung = request.Content;
        schedule.phong_hoc = request.Room;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(schedule, "Cập nhật thời khóa biểu thành công"));
    }

    [HttpDelete("schedules/{id:long}")]
    public async Task<IActionResult> DeleteSchedule(long id)
    {
        var schedule = await _dbContext.buoi_hocs.FindAsync(id);
        if (schedule is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy buổi học"));
        _dbContext.buoi_hocs.Remove(schedule);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Xóa thời khóa biểu thành công"));
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetStudents()
    {
        var data = await _dbContext.hoc_viens
            .AsNoTracking()
            .Include(item => item.nguoi_dung)
            .Include(item => item.lop_hoc_hoc_viens)
            .ThenInclude(item => item.lop_hoc)
            .Include(item => item.ho_so_dang_kies)
            .Select(item => new
            {
                item.id,
                item.ho_ten,
                item.ngay_sinh,
                item.gioi_tinh,
                item.cccd,
                item.dia_chi,
                item.created_at,
                user = new { item.nguoi_dung.id, item.nguoi_dung.ten_dang_nhap, item.nguoi_dung.email, item.nguoi_dung.so_dien_thoai, item.nguoi_dung.trang_thai },
                classes = item.lop_hoc_hoc_viens.Select(enrollment => new
                {
                    enrollment.id,
                    enrollment.lop_hoc_id,
                    enrollment.trang_thai,
                    enrollment.ngay_vao_lop,
                    class_name = enrollment.lop_hoc.ten_lop
                }),
                profiles = item.ho_so_dang_kies.Select(profile => new
                {
                    profile.id,
                    profile.ma_ho_so,
                    profile.trang_thai,
                    profile.ngay_nop
                })
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách học viên thành công"));
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] UpsertStudentRequest request)
    {
        var now = DateTime.UtcNow;
        var username = string.IsNullOrWhiteSpace(request.Username) ? request.Email : request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var existed = await _dbContext.nguoi_dungs.AnyAsync(item => item.ten_dang_nhap == username || item.email == email || (!string.IsNullOrWhiteSpace(request.PhoneNumber) && item.so_dien_thoai == request.PhoneNumber));
        if (existed) return Conflict(ApiResponseFactory.Fail("Tên đăng nhập, email hoặc số điện thoại đã tồn tại."));

        var cccdExists = !string.IsNullOrWhiteSpace(request.Cccd) && await _dbContext.hoc_viens.AnyAsync(item => item.cccd == request.Cccd);
        if (cccdExists) return Conflict(ApiResponseFactory.Fail("CCCD học viên đã tồn tại."));

        var user = new nguoi_dung
        {
            ten_dang_nhap = username,
            email = email,
            so_dien_thoai = request.PhoneNumber,
            trang_thai = request.Status ?? "hoat_dong",
            created_at = now,
            updated_at = now
        };
        user.mat_khau_hash = new PasswordHasher<nguoi_dung>().HashPassword(user, string.IsNullOrWhiteSpace(request.Password) ? "Student@123" : request.Password);
        _dbContext.nguoi_dungs.Add(user);
        await _dbContext.SaveChangesAsync();

        var student = new hoc_vien
        {
            nguoi_dung_id = user.id,
            ho_ten = request.FullName,
            ngay_sinh = request.DateOfBirth,
            gioi_tinh = request.Gender,
            cccd = request.Cccd,
            dia_chi = request.Address,
            anh_chan_dung = request.AvatarUrl,
            created_at = now
        };
        _dbContext.hoc_viens.Add(student);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Created(new { student.id, student.nguoi_dung_id }, "Tạo học viên thành công"));
    }

    [HttpPut("students/{id:long}")]
    public async Task<IActionResult> UpdateStudent(long id, [FromBody] UpsertStudentRequest request)
    {
        var student = await _dbContext.hoc_viens.Include(item => item.nguoi_dung).FirstOrDefaultAsync(item => item.id == id);
        if (student is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy học viên"));

        var email = request.Email.Trim().ToLowerInvariant();
        var username = string.IsNullOrWhiteSpace(request.Username) ? student.nguoi_dung.ten_dang_nhap : request.Username.Trim();
        var duplicateUser = await _dbContext.nguoi_dungs.AnyAsync(item => item.id != student.nguoi_dung_id && (item.ten_dang_nhap == username || item.email == email || (!string.IsNullOrWhiteSpace(request.PhoneNumber) && item.so_dien_thoai == request.PhoneNumber)));
        if (duplicateUser) return Conflict(ApiResponseFactory.Fail("Tên đăng nhập, email hoặc số điện thoại đã tồn tại."));
        var duplicateCccd = !string.IsNullOrWhiteSpace(request.Cccd) && await _dbContext.hoc_viens.AnyAsync(item => item.id != id && item.cccd == request.Cccd);
        if (duplicateCccd) return Conflict(ApiResponseFactory.Fail("CCCD học viên đã tồn tại."));

        student.ho_ten = request.FullName;
        student.ngay_sinh = request.DateOfBirth;
        student.gioi_tinh = request.Gender;
        student.cccd = request.Cccd;
        student.dia_chi = request.Address;
        student.anh_chan_dung = request.AvatarUrl;
        student.nguoi_dung.ten_dang_nhap = username;
        student.nguoi_dung.email = email;
        student.nguoi_dung.so_dien_thoai = request.PhoneNumber;
        student.nguoi_dung.trang_thai = request.Status ?? student.nguoi_dung.trang_thai;
        student.nguoi_dung.updated_at = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Password)) student.nguoi_dung.mat_khau_hash = new PasswordHasher<nguoi_dung>().HashPassword(student.nguoi_dung, request.Password);
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { student.id, student.nguoi_dung_id }, "Cập nhật học viên thành công"));
    }

    [HttpDelete("students/{id:long}")]
    public async Task<IActionResult> DeleteStudent(long id)
    {
        var student = await _dbContext.hoc_viens.Include(item => item.nguoi_dung).FirstOrDefaultAsync(item => item.id == id);
        if (student is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy học viên"));
        student.nguoi_dung.trang_thai = "tam_khoa";
        student.nguoi_dung.updated_at = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(new { id }, "Khóa tài khoản học viên thành công"));
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> GetTeachers()
    {
        var data = await _dbContext.nguoi_dungs
            .AsNoTracking()
            .Include(item => item.nguoi_dung_vai_tros)
            .ThenInclude(item => item.vai_tro)
            .Where(item => item.nguoi_dung_vai_tros.Any(role => role.vai_tro.ma_vai_tro == "giao_vien" || role.vai_tro.ma_vai_tro == "teacher"))
            .Select(item => new
            {
                item.id,
                item.ten_dang_nhap,
                item.email,
                item.so_dien_thoai,
                item.trang_thai,
                item.created_at,
                class_count = _dbContext.lop_hocs.Count(classItem => classItem.giao_vien_id == item.id)
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách giáo viên thành công"));
    }

    [HttpGet("topics")]
    public async Task<IActionResult> GetTopics()
    {
        var data = await _dbContext.chu_de_cau_hois
            .AsNoTracking()
            .Select(item => new
            {
                item.id,
                item.ma_chu_de,
                item.ten_chu_de,
                item.mo_ta,
                question_count = item.cau_hois.Count
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách chủ đề câu hỏi thành công"));
    }

    [HttpGet("exam-periods")]
    public async Task<IActionResult> GetExamPeriods()
    {
        var data = await _dbContext.ky_this.AsNoTracking().Include(item => item.ca_this).ToListAsync();
        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách kỳ thi thành công"));
    }

    [HttpGet("exam-results")]
    public async Task<IActionResult> GetExamResults()
    {
        var data = await _dbContext.exam_results
            .AsNoTracking()
            .Include(item => item.hoc_vien)
            .Include(item => item.bai_thi)
            .Select(item => new
            {
                item.id,
                item.bai_thi_id,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                item.tong_so_cau,
                item.so_cau_dung,
                item.diem,
                item.ket_qua,
                item.xac_nhan_luc,
                item.created_at
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách kết quả thi thành công"));
    }

    [HttpGet("certificates")]
    public async Task<IActionResult> GetCertificates()
    {
        var data = await _dbContext.certificates
            .AsNoTracking()
            .Include(item => item.hoc_vien)
            .Include(item => item.exam_result)
            .Select(item => new
            {
                item.id,
                item.ma_chung_chi,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                item.exam_result_id,
                item.ngay_cap,
                item.ngay_het_han,
                item.trang_thai,
                item.created_at
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách giấy phép/chứng chỉ thành công"));
    }

    [HttpGet("course-registrations")]
    public async Task<IActionResult> GetCourseRegistrations()
    {
        var data = await _dbContext.dang_ky_khoa_hocs
            .AsNoTracking()
            .Include(item => item.hoc_vien)
            .Include(item => item.khoa_hoc)
            .Select(item => new
            {
                item.id,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                item.khoa_hoc_id,
                course_code = item.khoa_hoc.ma_khoa_hoc,
                course_name = item.khoa_hoc.ten_khoa_hoc,
                course_fee = item.khoa_hoc.hoc_phi,
                item.ngay_dang_ky,
                item.trang_thai,
                item.nguoi_duyet_id,
                item.ngay_duyet,
                assigned_classes = _dbContext.lop_hoc_hoc_viens
                    .Where(member => member.hoc_vien_id == item.hoc_vien_id && member.lop_hoc.khoa_hoc_id == item.khoa_hoc_id)
                    .Select(member => new
                    {
                        member.lop_hoc_id,
                        class_code = member.lop_hoc.ma_lop,
                        class_name = member.lop_hoc.ten_lop,
                        member.ngay_vao_lop,
                        member.trang_thai
                    })
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách đăng ký khóa học thành công"));
    }

    [HttpGet("exam-registrations")]
    public async Task<IActionResult> GetExamRegistrations()
    {
        var data = await _dbContext.dang_ky_du_this
            .AsNoTracking()
            .Include(item => item.hoc_vien)
            .Include(item => item.ca_thi)
            .ThenInclude(item => item.ky_thi)
            .Select(item => new
            {
                item.id,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                item.ca_thi_id,
                exam_session_code = item.ca_thi.ma_ca_thi,
                exam_session_name = item.ca_thi.ten_ca_thi,
                exam_period_name = item.ca_thi.ky_thi.ten_ky_thi,
                exam_date = item.ca_thi.ky_thi.ngay_thi,
                item.ngay_dang_ky,
                item.trang_thai,
                item.nguoi_duyet_id,
                item.ngay_duyet
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách đăng ký dự thi thành công"));
    }

    [HttpPatch("exam-registrations/{registrationId:long}/approve")]
    public async Task<IActionResult> ApproveExamRegistration(long registrationId)
    {
        var registration = await _dbContext.dang_ky_du_this.FindAsync(registrationId);
        if (registration is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đăng ký dự thi"));
        registration.trang_thai = "da_duyet";
        registration.nguoi_duyet_id = GetCurrentUserId();
        registration.ngay_duyet = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(registration, "Duyệt đăng ký dự thi thành công"));
    }

    [HttpGet("fee-types")]
    public async Task<IActionResult> GetFeeTypes()
    {
        var data = await _dbContext.loai_khoan_thus.AsNoTracking().OrderBy(item => item.ma_loai).ToListAsync();
        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách loại khoản thu thành công"));
    }

    [HttpGet("receipts")]
    public async Task<IActionResult> GetReceipts()
    {
        var data = await _dbContext.phieu_thus
            .AsNoTracking()
            .Include(item => item.hoc_vien)
            .Include(item => item.chi_tiet_phieu_thus)
            .ThenInclude(item => item.loai_khoan_thu)
            .Select(item => new
            {
                item.id,
                item.ma_phieu_thu,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                item.tong_tien,
                item.ngay_thu,
                item.trang_thai,
                details = item.chi_tiet_phieu_thus.Select(detail => new
                {
                    detail.id,
                    detail.loai_khoan_thu_id,
                    fee_type_code = detail.loai_khoan_thu.ma_loai,
                    fee_type_name = detail.loai_khoan_thu.ten_loai,
                    detail.so_tien,
                    detail.ghi_chu
                })
            })
            .ToListAsync();

        return Ok(ApiResponseFactory.Success(data, "Lấy danh sách phiếu thu thành công"));
    }

    [HttpPatch("receipts/{id:long}/confirm")]
    public async Task<IActionResult> ConfirmReceipt(long id)
    {
        var receipt = await _dbContext.phieu_thus.FindAsync(id);
        if (receipt is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy phiếu thu"));
        receipt.trang_thai = "da_xac_nhan";
        receipt.nguoi_xac_nhan_id = GetCurrentUserId();
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(receipt, "Xác nhận phiếu thu thành công"));
    }

    [HttpPatch("receipts/{id:long}/cancel")]
    public async Task<IActionResult> CancelReceipt(long id)
    {
        var receipt = await _dbContext.phieu_thus.FindAsync(id);
        if (receipt is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy phiếu thu"));
        receipt.trang_thai = "da_huy";
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(receipt, "Hủy phiếu thu thành công"));
    }

    [HttpPost("classes/{classId:long}/students/{studentId:long}/approve")]
    public async Task<IActionResult> ApproveStudentToClass(long classId, long studentId)
    {
        var classExists = await _dbContext.lop_hocs.AnyAsync(item => item.id == classId);
        var studentExists = await _dbContext.hoc_viens.AnyAsync(item => item.id == studentId);
        if (!classExists || !studentExists) return NotFound(ApiResponseFactory.Fail("Không tìm thấy lớp học hoặc học viên"));

        var enrollment = await _dbContext.lop_hoc_hoc_viens.FirstOrDefaultAsync(item => item.lop_hoc_id == classId && item.hoc_vien_id == studentId);
        if (enrollment is null)
        {
            enrollment = new lop_hoc_hoc_vien { lop_hoc_id = classId, hoc_vien_id = studentId, ngay_vao_lop = DateOnly.FromDateTime(DateTime.UtcNow), trang_thai = "dang_hoc" };
            _dbContext.lop_hoc_hoc_viens.Add(enrollment);
        }
        else
        {
            enrollment.trang_thai = "dang_hoc";
            enrollment.ngay_vao_lop ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(enrollment, "Duyệt học viên vào lớp thành công"));
    }

    [HttpPatch("course-registrations/{registrationId:long}/approve")]
    public async Task<IActionResult> ApproveCourseRegistration(long registrationId)
    {
        var registration = await _dbContext.dang_ky_khoa_hocs.FindAsync(registrationId);
        if (registration is null) return NotFound(ApiResponseFactory.Fail("Không tìm thấy đăng ký khóa học"));
        registration.trang_thai = "da_duyet";
        registration.nguoi_duyet_id = GetCurrentUserId();
        registration.ngay_duyet = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return Ok(ApiResponseFactory.Success(registration, "Duyệt đăng ký khóa học thành công"));
    }

    private long? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private static AdminQuestionResponse MapQuestionResponse(cau_hoi question)
    {
        return new AdminQuestionResponse(
            question.id,
            question.chu_de_id,
            question.noi_dung,
            question.giai_thich_dap_an,
            question.loai_cau_hoi,
            question.muc_do,
            question.la_cau_diem_liet,
            question.trang_thai,
            question.dap_ans
                .OrderBy(answer => answer.thu_tu)
                .Select(answer => new AdminAnswerResponse(
                    answer.id,
                    answer.cau_hoi_id,
                    answer.noi_dung,
                    answer.la_dap_an_dung,
                    answer.thu_tu))
                .ToList());
    }
}

public sealed record UpdateStatusRequest(string Status);
public sealed record AssignRoleRequest(long? RoleId, string? RoleCode);
public sealed record UpsertQuestionRequest(long TopicId, string Content, string? Explanation, string? QuestionType, string? Level, bool IsCritical, string? Status);
public sealed record UpsertStudentRequest(string? Username, string Email, string? PhoneNumber, string? Password, string FullName, DateOnly? DateOfBirth, string? Gender, string? Cccd, string? Address, string? AvatarUrl, string? Status);
public sealed record AdminQuestionResponse(long id, long chu_de_id, string noi_dung, string? giai_thich_dap_an, string loai_cau_hoi, string? muc_do, bool la_cau_diem_liet, string trang_thai, List<AdminAnswerResponse> dap_ans);
public sealed record AdminAnswerResponse(long id, long cau_hoi_id, string noi_dung, bool la_dap_an_dung, int thu_tu);
public sealed record UpsertAnswerRequest(long QuestionId, string Content, bool IsCorrect, int Order);
public sealed record UpsertExamRequest(string Code, string Name, long ExamPeriodId, int TotalQuestions, int DurationMinutes, string? Status, string? Type);
public sealed record AddExamQuestionRequest(long QuestionId, int Order);
public sealed record UpsertCourseRequest(string Code, string Name, string? Description, decimal Fee, int? Duration, string? Status);
public sealed record UpsertClassRequest(long CourseId, string Code, string Name, long? TeacherId, DateOnly? StartDate, DateOnly? EndDate, int MaxStudents, string? Status);
public sealed record UpsertScheduleRequest(long ClassId, string Name, DateOnly StudyDate, TimeOnly StartTime, TimeOnly EndTime, string? Content, string? Room);
