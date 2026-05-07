using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Services.Admin;

public interface IStudentApprovalService
{
    Task<IReadOnlyList<StudentApprovalQueueItem>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<StudentApprovalResult> ApproveAsync(long courseRegistrationId, long? approverId, CancellationToken cancellationToken = default);
}

public sealed class StudentApprovalService : IStudentApprovalService
{
    private readonly ApplicationDbContext _dbContext;

    public StudentApprovalService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<StudentApprovalQueueItem>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingRegistrations = await _dbContext.dang_ky_khoa_hocs
            .AsNoTracking()
            .Include(item => item.hoc_vien)
                .ThenInclude(student => student.nguoi_dung)
            .Include(item => item.khoa_hoc)
            .Where(item => item.trang_thai != "da_duyet")
            .OrderBy(item => item.ngay_dang_ky)
            .Select(item => new
            {
                item.id,
                item.hoc_vien_id,
                student_name = item.hoc_vien.ho_ten,
                student_email = item.hoc_vien.nguoi_dung.email,
                student_phone = item.hoc_vien.nguoi_dung.so_dien_thoai,
                item.khoa_hoc_id,
                course_code = item.khoa_hoc.ma_khoa_hoc,
                course_name = item.khoa_hoc.ten_khoa_hoc,
                course_fee = item.khoa_hoc.hoc_phi,
                item.ngay_dang_ky,
                item.trang_thai,
                profile_count = _dbContext.ho_so_dang_kies.Count(profile => profile.hoc_vien_id == item.hoc_vien_id),
                pending_profile_count = _dbContext.ho_so_dang_kies.Count(profile => profile.hoc_vien_id == item.hoc_vien_id && profile.trang_thai != "da_duyet"),
                approved_profile_count = _dbContext.ho_so_dang_kies.Count(profile => profile.hoc_vien_id == item.hoc_vien_id && profile.trang_thai == "da_duyet"),
                existing_class = _dbContext.lop_hoc_hoc_viens
                    .Where(member => member.hoc_vien_id == item.hoc_vien_id && member.lop_hoc.khoa_hoc_id == item.khoa_hoc_id)
                    .OrderBy(member => member.trang_thai == "dang_hoc" ? 0 : 1)
                    .ThenByDescending(member => member.id)
                    .Select(member => new StudentApprovalClassOption(member.lop_hoc_id, member.lop_hoc.ma_lop, member.lop_hoc.ten_lop, member.trang_thai, null, true))
                    .FirstOrDefault(),
                available_class = _dbContext.lop_hocs
                    .Where(classItem => classItem.khoa_hoc_id == item.khoa_hoc_id)
                    .Select(classItem => new
                    {
                        classItem.id,
                        classItem.ma_lop,
                        classItem.ten_lop,
                        classItem.trang_thai,
                        classItem.ngay_bat_dau,
                        classItem.si_so_toi_da,
                        current_students = _dbContext.lop_hoc_hoc_viens.Count(member => member.lop_hoc_id == classItem.id && member.trang_thai == "dang_hoc"),
                        already_member = _dbContext.lop_hoc_hoc_viens.Any(member => member.lop_hoc_id == classItem.id && member.hoc_vien_id == item.hoc_vien_id)
                    })
                    .Where(classItem => !classItem.already_member && (classItem.si_so_toi_da <= 0 || classItem.current_students < classItem.si_so_toi_da))
                    .OrderBy(classItem => classItem.trang_thai == "dang_mo" ? 0 : 1)
                    .ThenBy(classItem => classItem.current_students)
                    .ThenBy(classItem => classItem.ngay_bat_dau)
                    .ThenBy(classItem => classItem.id)
                    .Select(classItem => new StudentApprovalClassOption(classItem.id, classItem.ma_lop, classItem.ten_lop, classItem.trang_thai, classItem.current_students, false))
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return pendingRegistrations.Select(item =>
        {
            var selectedClass = item.existing_class ?? item.available_class;
            var blockers = new List<string>();
            if (selectedClass is null) blockers.Add("Không có lớp còn sĩ số thuộc khóa học đăng ký.");
            if (item.profile_count == 0) blockers.Add("Học viên chưa có hồ sơ đăng ký.");

            return new StudentApprovalQueueItem(
                item.id,
                item.hoc_vien_id,
                item.student_name,
                item.student_email,
                item.student_phone,
                item.khoa_hoc_id,
                item.course_code,
                item.course_name,
                item.course_fee,
                item.ngay_dang_ky,
                item.trang_thai,
                item.profile_count,
                item.pending_profile_count,
                item.approved_profile_count,
                selectedClass,
                blockers.Count == 0,
                blockers);
        }).ToList();
    }

    public async Task<StudentApprovalResult> ApproveAsync(long courseRegistrationId, long? approverId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var approvedDate = DateOnly.FromDateTime(now);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var registration = await _dbContext.dang_ky_khoa_hocs
            .Include(item => item.hoc_vien)
            .Include(item => item.khoa_hoc)
            .FirstOrDefaultAsync(item => item.id == courseRegistrationId, cancellationToken);

        if (registration is null)
        {
            return StudentApprovalResult.NotFound(courseRegistrationId);
        }

        var selectedClass = await FindExistingClassAsync(registration.hoc_vien_id, registration.khoa_hoc_id, cancellationToken)
            ?? await FindAvailableClassAsync(registration.hoc_vien_id, registration.khoa_hoc_id, cancellationToken);

        if (selectedClass is null)
        {
            return StudentApprovalResult.Failed(courseRegistrationId, "Không có lớp đang mở/còn sĩ số thuộc khóa học để gán học viên.");
        }

        var classMember = await _dbContext.lop_hoc_hoc_viens
            .FirstOrDefaultAsync(item => item.lop_hoc_id == selectedClass.id && item.hoc_vien_id == registration.hoc_vien_id, cancellationToken);

        var insertedClassMember = false;
        if (classMember is null)
        {
            classMember = new lop_hoc_hoc_vien
            {
                lop_hoc_id = selectedClass.id,
                hoc_vien_id = registration.hoc_vien_id,
                ngay_vao_lop = approvedDate,
                trang_thai = "dang_hoc"
            };
            _dbContext.lop_hoc_hoc_viens.Add(classMember);
            insertedClassMember = true;
        }
        else
        {
            classMember.trang_thai = "dang_hoc";
            classMember.ngay_vao_lop ??= approvedDate;
        }

        registration.trang_thai = "da_duyet";
        registration.nguoi_duyet_id = approverId;
        registration.ngay_duyet ??= now;

        var profiles = await _dbContext.ho_so_dang_kies
            .Where(item => item.hoc_vien_id == registration.hoc_vien_id && item.trang_thai != "da_duyet")
            .ToListAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            profile.trang_thai = "da_duyet";
            profile.nguoi_duyet_id ??= approverId;
            profile.ngay_nop ??= now;
            profile.ngay_duyet ??= now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StudentApprovalResult.Success(courseRegistrationId, registration.hoc_vien_id, selectedClass.id, selectedClass.ma_lop, selectedClass.ten_lop, insertedClassMember, profiles.Count);
    }

    private async Task<lop_hoc?> FindExistingClassAsync(long studentId, long courseId, CancellationToken cancellationToken)
    {
        return await _dbContext.lop_hoc_hoc_viens
            .Where(member => member.hoc_vien_id == studentId && member.lop_hoc.khoa_hoc_id == courseId)
            .OrderBy(member => member.trang_thai == "dang_hoc" ? 0 : 1)
            .ThenByDescending(member => member.id)
            .Select(member => member.lop_hoc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<lop_hoc?> FindAvailableClassAsync(long studentId, long courseId, CancellationToken cancellationToken)
    {
        return await _dbContext.lop_hocs
            .Where(classItem => classItem.khoa_hoc_id == courseId)
            .Select(classItem => new
            {
                Class = classItem,
                CurrentStudents = _dbContext.lop_hoc_hoc_viens.Count(member => member.lop_hoc_id == classItem.id && member.trang_thai == "dang_hoc"),
                AlreadyMember = _dbContext.lop_hoc_hoc_viens.Any(member => member.lop_hoc_id == classItem.id && member.hoc_vien_id == studentId)
            })
            .Where(item => !item.AlreadyMember && (item.Class.si_so_toi_da <= 0 || item.CurrentStudents < item.Class.si_so_toi_da))
            .OrderBy(item => item.Class.trang_thai == "dang_mo" ? 0 : 1)
            .ThenBy(item => item.CurrentStudents)
            .ThenBy(item => item.Class.ngay_bat_dau)
            .ThenBy(item => item.Class.id)
            .Select(item => item.Class)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed record StudentApprovalQueueItem(
    long id,
    long hoc_vien_id,
    string student_name,
    string student_email,
    string? student_phone,
    long khoa_hoc_id,
    string course_code,
    string course_name,
    decimal course_fee,
    DateTime ngay_dang_ky,
    string trang_thai,
    int profile_count,
    int pending_profile_count,
    int approved_profile_count,
    StudentApprovalClassOption? selected_class,
    bool can_approve,
    IReadOnlyList<string> blockers);

public sealed record StudentApprovalClassOption(long id, string ma_lop, string ten_lop, string trang_thai, int? current_students, bool is_existing_member);

public sealed record StudentApprovalResult(bool success, bool not_found, long course_registration_id, long? hoc_vien_id, long? lop_hoc_id, string? class_code, string? class_name, bool inserted_class_member, int approved_profiles, string? message)
{
    public static StudentApprovalResult Success(long registrationId, long studentId, long classId, string classCode, string className, bool insertedClassMember, int approvedProfiles)
        => new(true, false, registrationId, studentId, classId, classCode, className, insertedClassMember, approvedProfiles, "Duyệt học viên thành công");

    public static StudentApprovalResult Failed(long registrationId, string message)
        => new(false, false, registrationId, null, null, null, null, false, 0, message);

    public static StudentApprovalResult NotFound(long registrationId)
        => new(false, true, registrationId, null, null, null, null, false, 0, "Không tìm thấy đăng ký khóa học");
}
