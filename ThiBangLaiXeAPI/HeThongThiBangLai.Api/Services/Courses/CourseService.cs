using HeThongThiBangLai.Api.Common.Exceptions;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.DTOs.Courses;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HeThongThiBangLai.Api.Services.Courses;

public sealed class CourseService : ICourseService
{
    private const string DefaultCourseImage = "/media/courses/default.jpg";

    private readonly ApplicationDbContext _dbContext;

    public CourseService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<PagedList<CourseListItemDto>>> GetCoursesAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.khoa_hocs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(course => course.ma_khoa_hoc.Contains(keyword) || course.ten_khoa_hoc.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var dbStatus = ToDatabaseCourseStatus(status);
            query = query.Where(course => course.trang_thai == dbStatus);
        }

        var totalItems = await query.CountAsync();
        var courses = await query
            .OrderBy(course => course.id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(course => new
            {
                Course = course,
                PrimaryClass = course.lop_hocs
                    .OrderByDescending(classroom => classroom.trang_thai == "dang_mo")
                    .ThenBy(classroom => classroom.ngay_bat_dau ?? DateOnly.MaxValue)
                    .ThenBy(classroom => classroom.id)
                    .Select(classroom => new
                    {
                        classroom.id,
                        classroom.si_so_toi_da,
                        classroom.ngay_bat_dau,
                        classroom.ngay_ket_thuc,
                        CurrentStudents = classroom.lop_hoc_hoc_viens.Count(student => student.trang_thai == "dang_hoc"),
                        FirstSchedule = classroom.buoi_hocs
                            .OrderBy(session => session.ngay_hoc)
                            .ThenBy(session => session.gio_bat_dau)
                            .Select(session => new
                            {
                                session.ngay_hoc,
                                session.gio_bat_dau
                            })
                            .FirstOrDefault()
                    })
                    .FirstOrDefault()
            })
            .ToListAsync();

        var items = courses.Select(row => new CourseListItemDto
        {
            CourseId = row.Course.id,
            MaKhoaHoc = row.Course.ma_khoa_hoc,
            TenKhoaHoc = row.Course.ten_khoa_hoc,
            LoaiBangLai = InferLicenseType(row.Course.ma_khoa_hoc, row.Course.ten_khoa_hoc),
            MoTaNgan = row.Course.mo_ta,
            HocPhi = row.Course.hoc_phi,
            SoBuoiHoc = row.Course.thoi_luong ?? 0,
            SoLuongToiDa = row.PrimaryClass?.si_so_toi_da ?? 0,
            SoLuongHienTai = row.PrimaryClass?.CurrentStudents ?? 0,
            NgayBatDau = row.PrimaryClass?.ngay_bat_dau,
            NgayKetThuc = row.PrimaryClass?.ngay_ket_thuc,
            LichHocTomTat = row.PrimaryClass?.FirstSchedule is null
                ? null
                : $"T{ToVietnameseDayOfWeek(row.PrimaryClass.FirstSchedule.ngay_hoc)} {row.PrimaryClass.FirstSchedule.gio_bat_dau:HH\\:mm}",
            TrangThai = ToApiCourseStatus(row.Course.trang_thai),
            HinhAnh = BuildCourseImage(row.Course.ma_khoa_hoc),
            IsOpenForRegistration = IsOpenForRegistration(row.Course.trang_thai, row.PrimaryClass?.si_so_toi_da ?? 0, row.PrimaryClass?.CurrentStudents ?? 0)
        }).ToList();

        var paged = new PagedList<CourseListItemDto>(items, totalItems, page, pageSize);
        return ApiResponseFactory.SuccessPaged(paged, "Lấy danh sách khóa học thành công");
    }

    public async Task<ApiResponse<CourseDetailDto>> GetCourseByIdAsync(long courseId)
    {
        var course = await _dbContext.khoa_hocs
            .AsNoTracking()
            .Where(item => item.id == courseId)
            .Select(item => new
            {
                Course = item,
                PrimaryClass = item.lop_hocs
                    .OrderByDescending(classroom => classroom.trang_thai == "dang_mo")
                    .ThenBy(classroom => classroom.ngay_bat_dau ?? DateOnly.MaxValue)
                    .ThenBy(classroom => classroom.id)
                    .Select(classroom => new
                    {
                        classroom.id,
                        classroom.si_so_toi_da,
                        classroom.ngay_bat_dau,
                        classroom.ngay_ket_thuc,
                        Teacher = classroom.giao_vien == null ? null : new
                        {
                            classroom.giao_vien.id,
                            classroom.giao_vien.ten_dang_nhap,
                            classroom.giao_vien.so_dien_thoai,
                            HoTenHocVien = classroom.giao_vien.hoc_vien == null ? null : classroom.giao_vien.hoc_vien.ho_ten
                        },
                        CurrentStudents = classroom.lop_hoc_hoc_viens.Count(student => student.trang_thai == "dang_hoc"),
                        Schedules = classroom.buoi_hocs
                            .OrderBy(session => session.ngay_hoc)
                            .ThenBy(session => session.gio_bat_dau)
                            .Select(session => new
                            {
                                session.ngay_hoc,
                                session.gio_bat_dau,
                                session.gio_ket_thuc,
                                session.phong_hoc
                            })
                            .ToList()
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (course is null)
        {
            return ApiResponseFactory.Fail<CourseDetailDto>("Không tìm thấy khóa học");
        }

        var dto = new CourseDetailDto
        {
            CourseId = course.Course.id,
            MaKhoaHoc = course.Course.ma_khoa_hoc,
            TenKhoaHoc = course.Course.ten_khoa_hoc,
            LoaiBangLai = InferLicenseType(course.Course.ma_khoa_hoc, course.Course.ten_khoa_hoc),
            MoTa = course.Course.mo_ta,
            HocPhi = course.Course.hoc_phi,
            SoBuoiHoc = course.Course.thoi_luong ?? 0,
            SoLuongToiDa = course.PrimaryClass?.si_so_toi_da ?? 0,
            SoLuongHienTai = course.PrimaryClass?.CurrentStudents ?? 0,
            NgayBatDau = course.PrimaryClass?.ngay_bat_dau,
            NgayKetThuc = course.PrimaryClass?.ngay_ket_thuc,
            TrangThai = ToApiCourseStatus(course.Course.trang_thai),
            GiaoVienChinh = course.PrimaryClass?.Teacher is null
                ? null
                : new CourseTeacherDto
                {
                    TeacherId = course.PrimaryClass.Teacher.id,
                    HoTen = course.PrimaryClass.Teacher.HoTenHocVien ?? course.PrimaryClass.Teacher.ten_dang_nhap,
                    SoDienThoai = course.PrimaryClass.Teacher.so_dien_thoai
                },
            LichHocMau = course.PrimaryClass?.Schedules
                .Select(schedule => new CourseScheduleDto
                {
                    ThuTrongTuan = ToVietnameseDayOfWeek(schedule.ngay_hoc),
                    GioBatDau = schedule.gio_bat_dau.ToString("HH\\:mm"),
                    GioKetThuc = schedule.gio_ket_thuc.ToString("HH\\:mm"),
                    DiaDiem = schedule.phong_hoc
                })
                .ToList() ?? new List<CourseScheduleDto>(),
            Classes = (await GetCourseClassesAsync(courseId)).Data ?? new List<CourseClassDto>(),
            HinhAnh = BuildCourseImage(course.Course.ma_khoa_hoc)
        };

        return ApiResponseFactory.Success(dto, "Lấy chi tiết khóa học thành công");
    }

    public async Task<ApiResponse<List<CourseClassDto>>> GetCourseClassesAsync(long courseId)
    {
        var courseExists = await _dbContext.khoa_hocs
            .AsNoTracking()
            .AnyAsync(course => course.id == courseId);

        if (!courseExists)
        {
            return ApiResponseFactory.Fail<List<CourseClassDto>>("Không tìm thấy khóa học");
        }

        var classes = await _dbContext.lop_hocs
            .AsNoTracking()
            .Where(classroom => classroom.khoa_hoc_id == courseId)
            .OrderByDescending(classroom => classroom.trang_thai == "dang_mo")
            .ThenBy(classroom => classroom.ngay_bat_dau ?? DateOnly.MaxValue)
            .ThenBy(classroom => classroom.id)
            .Select(classroom => new
            {
                classroom.id,
                classroom.ma_lop,
                classroom.ten_lop,
                classroom.si_so_toi_da,
                classroom.ngay_bat_dau,
                classroom.ngay_ket_thuc,
                classroom.trang_thai,
                CurrentStudents = classroom.lop_hoc_hoc_viens.Count(student => student.trang_thai == "dang_hoc"),
                Teacher = classroom.giao_vien == null ? null : new
                {
                    classroom.giao_vien.id,
                    classroom.giao_vien.ten_dang_nhap,
                    classroom.giao_vien.so_dien_thoai,
                    HoTenHocVien = classroom.giao_vien.hoc_vien == null ? null : classroom.giao_vien.hoc_vien.ho_ten
                },
                Schedules = classroom.buoi_hocs
                    .OrderBy(session => session.ngay_hoc)
                    .ThenBy(session => session.gio_bat_dau)
                    .Select(session => new
                    {
                        session.ngay_hoc,
                        session.gio_bat_dau,
                        session.gio_ket_thuc,
                        session.phong_hoc
                    })
                    .ToList()
            })
            .ToListAsync();

        var dtos = classes.Select(classroom => new CourseClassDto
        {
            ClassId = classroom.id,
            MaLop = classroom.ma_lop,
            TenLop = classroom.ten_lop,
            SiSoToiDa = classroom.si_so_toi_da,
            SoLuongHienTai = classroom.CurrentStudents,
            NgayBatDau = classroom.ngay_bat_dau,
            NgayKetThuc = classroom.ngay_ket_thuc,
            TrangThai = ToApiCourseStatus(classroom.trang_thai),
            IsOpenForRegistration = IsOpenForRegistration(classroom.trang_thai, classroom.si_so_toi_da, classroom.CurrentStudents),
            GiaoVien = classroom.Teacher is null
                ? null
                : new CourseTeacherDto
                {
                    TeacherId = classroom.Teacher.id,
                    HoTen = classroom.Teacher.HoTenHocVien ?? classroom.Teacher.ten_dang_nhap,
                    SoDienThoai = classroom.Teacher.so_dien_thoai
                },
            LichHoc = classroom.Schedules.Select(schedule => new CourseScheduleDto
            {
                ThuTrongTuan = ToVietnameseDayOfWeek(schedule.ngay_hoc),
                GioBatDau = schedule.gio_bat_dau.ToString("HH\\:mm"),
                GioKetThuc = schedule.gio_ket_thuc.ToString("HH\\:mm"),
                DiaDiem = schedule.phong_hoc
            }).ToList()
        }).ToList();

        return ApiResponseFactory.Success(dtos, "Lấy danh sách lớp của khóa học thành công");
    }

    public async Task<ApiResponse<CourseRegistrationDto>> RegisterCourseAsync(CreateCourseRegistrationRequestDto request, long currentUserId)
    {
        var student = await _dbContext.hoc_viens.FirstOrDefaultAsync(item => item.nguoi_dung_id == currentUserId);
        if (student is null)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ học viên của tài khoản hiện tại");
        }

        var course = await _dbContext.khoa_hocs.FirstOrDefaultAsync(item => item.id == request.CourseId);
        if (course is null)
        {
            throw new NotFoundAppException("Không tìm thấy khóa học");
        }

        if (!string.Equals(course.trang_thai, "dang_mo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleAppException("Khóa học hiện không mở đăng ký", "COURSE_NOT_OPEN_FOR_REGISTRATION");
        }

        var classroom = await _dbContext.lop_hocs
            .Include(item => item.lop_hoc_hoc_viens)
            .FirstOrDefaultAsync(item => item.id == request.ClassId);
        if (classroom is null)
        {
            throw new NotFoundAppException("Không tìm thấy lớp học");
        }

        if (classroom.khoa_hoc_id != course.id)
        {
            throw new BusinessRuleAppException("Lớp học không thuộc khóa học đã chọn", "CLASS_NOT_BELONG_TO_COURSE");
        }

        if (!string.Equals(classroom.trang_thai, "dang_mo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleAppException("Lớp học hiện không mở đăng ký", "CLASS_NOT_OPEN_FOR_REGISTRATION");
        }

        var currentStudents = classroom.lop_hoc_hoc_viens.Count(member => string.Equals(member.trang_thai, "dang_hoc", StringComparison.OrdinalIgnoreCase));
        if (classroom.si_so_toi_da > 0 && currentStudents >= classroom.si_so_toi_da)
        {
            throw new BusinessRuleAppException("Lớp học đã đủ sĩ số", "CLASS_IS_FULL");
        }

        var hasRegistered = await _dbContext.dang_ky_khoa_hocs.AnyAsync(item => item.hoc_vien_id == student.id && item.khoa_hoc_id == request.CourseId);
        if (hasRegistered)
        {
            throw new ConflictAppException("Học viên đã đăng ký khóa học này", "COURSE_ALREADY_REGISTERED");
        }

        var isAlreadyInClass = await _dbContext.lop_hoc_hoc_viens.AnyAsync(item => item.hoc_vien_id == student.id && item.lop_hoc_id == request.ClassId);
        if (isAlreadyInClass)
        {
            throw new ConflictAppException("Học viên đã có trong lớp học này", "STUDENT_ALREADY_IN_CLASS");
        }

        var registration = new dang_ky_khoa_hoc
        {
            hoc_vien_id = student.id,
            khoa_hoc_id = request.CourseId,
            ngay_dang_ky = DateTime.UtcNow,
            trang_thai = "cho_duyet"
        };

        _dbContext.dang_ky_khoa_hocs.Add(registration);
        await _dbContext.SaveChangesAsync();

        var dto = new CourseRegistrationDto
        {
            RegistrationId = registration.id,
            StudentId = student.id,
            CourseId = course.id,
            ClassId = classroom.id,
            TenKhoaHoc = course.ten_khoa_hoc,
            TenLop = classroom.ten_lop,
            NgayDangKy = registration.ngay_dang_ky,
            TrangThai = ToApiRegistrationStatus(registration.trang_thai),
            GhiChu = request.GhiChu
        };

        return ApiResponseFactory.Created(dto, "Đăng ký lớp học thành công, vui lòng chờ duyệt");
    }

    public async Task<ApiResponse<CourseRegistrationDto>> ApproveRegistrationAsync(long registrationId, ApproveCourseRegistrationRequestDto request, long approverUserId)
    {
        var registration = await _dbContext.dang_ky_khoa_hocs
            .Include(item => item.khoa_hoc)
            .FirstOrDefaultAsync(item => item.id == registrationId);
        if (registration is null)
        {
            throw new NotFoundAppException("Không tìm thấy đăng ký khóa học");
        }

        if (!string.Equals(registration.trang_thai, "cho_duyet", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleAppException("Chỉ có thể duyệt đăng ký đang chờ duyệt", "REGISTRATION_NOT_PENDING");
        }

        var classroom = await _dbContext.lop_hocs
            .Include(item => item.lop_hoc_hoc_viens)
            .FirstOrDefaultAsync(item => item.id == request.ClassId);
        if (classroom is null)
        {
            throw new NotFoundAppException("Không tìm thấy lớp học");
        }

        if (classroom.khoa_hoc_id != registration.khoa_hoc_id)
        {
            throw new BusinessRuleAppException("Lớp học không thuộc khóa học của đăng ký", "CLASS_NOT_BELONG_TO_COURSE");
        }

        if (!string.Equals(classroom.trang_thai, "dang_mo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleAppException("Lớp học hiện không mở", "CLASS_NOT_OPEN");
        }

        var currentStudents = classroom.lop_hoc_hoc_viens.Count(member => string.Equals(member.trang_thai, "dang_hoc", StringComparison.OrdinalIgnoreCase));
        if (classroom.si_so_toi_da > 0 && currentStudents >= classroom.si_so_toi_da)
        {
            throw new BusinessRuleAppException("Lớp học đã đủ sĩ số", "CLASS_IS_FULL");
        }

        var isAlreadyInClass = await _dbContext.lop_hoc_hoc_viens.AnyAsync(item =>
            item.lop_hoc_id == classroom.id && item.hoc_vien_id == registration.hoc_vien_id);
        if (isAlreadyInClass)
        {
            throw new ConflictAppException("Học viên đã có trong lớp học này", "STUDENT_ALREADY_IN_CLASS");
        }

        registration.trang_thai = "da_duyet";
        registration.nguoi_duyet_id = approverUserId;
        registration.ngay_duyet = DateTime.UtcNow;

        var classMember = new lop_hoc_hoc_vien
        {
            lop_hoc_id = classroom.id,
            hoc_vien_id = registration.hoc_vien_id,
            ngay_vao_lop = DateOnly.FromDateTime(DateTime.UtcNow),
            trang_thai = "dang_hoc"
        };

        _dbContext.lop_hoc_hoc_viens.Add(classMember);
        await _dbContext.SaveChangesAsync();

        var dto = new CourseRegistrationDto
        {
            RegistrationId = registration.id,
            StudentId = registration.hoc_vien_id,
            CourseId = registration.khoa_hoc_id,
            ClassId = classroom.id,
            TenKhoaHoc = registration.khoa_hoc.ten_khoa_hoc,
            TenLop = classroom.ten_lop,
            NgayDangKy = registration.ngay_dang_ky,
            TrangThai = ToApiRegistrationStatus(registration.trang_thai),
            GhiChu = null
        };

        return ApiResponseFactory.Success(dto, "Duyệt đăng ký lớp học thành công");
    }

    public async Task<ApiResponse<PagedList<MyCourseRegistrationDto>>> GetMyRegistrationsAsync(long currentUserId, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var student = await _dbContext.hoc_viens.AsNoTracking().FirstOrDefaultAsync(item => item.nguoi_dung_id == currentUserId);
        if (student is null)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ học viên của tài khoản hiện tại");
        }

        var query = _dbContext.dang_ky_khoa_hocs
            .AsNoTracking()
            .Where(registration => registration.hoc_vien_id == student.id);

        var totalItems = await query.CountAsync();
        var rows = await query
            .OrderByDescending(registration => registration.ngay_dang_ky)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(registration => new
            {
                Registration = registration,
                Course = registration.khoa_hoc,
                Class = registration.khoa_hoc.lop_hocs
                    .Where(classroom => classroom.lop_hoc_hoc_viens.Any(member => member.hoc_vien_id == student.id))
                    .OrderByDescending(classroom => classroom.trang_thai == "dang_mo")
                    .ThenBy(classroom => classroom.ngay_bat_dau ?? DateOnly.MaxValue)
                    .FirstOrDefault(),
                HasPaidReceipt = _dbContext.phieu_thus.Any(receipt => receipt.hoc_vien_id == student.id && receipt.trang_thai == "da_xac_nhan")
            })
            .ToListAsync();

        var items = rows.Select(row => new MyCourseRegistrationDto
        {
            RegistrationId = row.Registration.id,
            CourseId = row.Course.id,
            TenKhoaHoc = row.Course.ten_khoa_hoc,
            LoaiBangLai = InferLicenseType(row.Course.ma_khoa_hoc, row.Course.ten_khoa_hoc),
            HocPhi = row.Course.hoc_phi,
            NgayDangKy = row.Registration.ngay_dang_ky,
            TrangThai = ToApiRegistrationStatus(row.Registration.trang_thai),
            PaymentStatus = row.HasPaidReceipt ? "DaThanhToan" : "ChuaThanhToan",
            ClassId = row.Class?.id,
            TenLop = row.Class?.ten_lop,
            NgayBatDau = row.Class?.ngay_bat_dau,
            NgayKetThuc = row.Class?.ngay_ket_thuc
        }).ToList();

        var paged = new PagedList<MyCourseRegistrationDto>(items, totalItems, page, pageSize);
        return ApiResponseFactory.SuccessPaged(paged, "Lấy danh sách đăng ký khóa học thành công");
    }

    private static bool IsOpenForRegistration(string status, int maxStudents, int currentStudents)
    {
        return string.Equals(status, "dang_mo", StringComparison.OrdinalIgnoreCase)
            && (maxStudents <= 0 || currentStudents < maxStudents);
    }

    private static string ToApiCourseStatus(string status)
    {
        return status switch
        {
            "dang_mo" => "DangMoDangKy",
            "tam_dung" => "TamDung",
            "da_dong" => "DaDong",
            "hoan_thanh" => "HoanThanh",
            _ => status
        };
    }

    private static string ToDatabaseCourseStatus(string status)
    {
        return status switch
        {
            "DangMoDangKy" => "dang_mo",
            "TamDung" => "tam_dung",
            "DaDong" => "da_dong",
            "HoanThanh" => "hoan_thanh",
            _ => status
        };
    }

    private static string ToApiRegistrationStatus(string status)
    {
        return status switch
        {
            "cho_duyet" => "ChoDuyet",
            "da_duyet" => "DaDuyet",
            "tu_choi" => "TuChoi",
            "da_huy" => "DaHuy",
            _ => status
        };
    }

    private static string InferLicenseType(string courseCode, string courseName)
    {
        var source = $"{courseCode} {courseName}".ToUpperInvariant();
        var knownTypes = new[] { "A1", "A2", "B1", "B2", "A", "C", "D", "E" };
        return knownTypes.FirstOrDefault(type => source.Contains(type, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static int ToVietnameseDayOfWeek(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Sunday ? 8 : (int)date.DayOfWeek + 1;
    }

    private static string BuildCourseImage(string courseCode)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
        {
            return DefaultCourseImage;
        }

        var slug = courseCode.Trim().ToLowerInvariant().Replace(" ", "-");
        return $"/media/courses/{slug}.jpg";
    }
}
