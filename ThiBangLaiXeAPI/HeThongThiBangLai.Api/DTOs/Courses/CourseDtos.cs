namespace HeThongThiBangLai.Api.DTOs.Courses;

public sealed class CourseListItemDto
{
    public long CourseId { get; set; }
    public string MaKhoaHoc { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string LoaiBangLai { get; set; } = string.Empty;
    public string? MoTaNgan { get; set; }
    public decimal HocPhi { get; set; }
    public int SoBuoiHoc { get; set; }
    public int SoLuongToiDa { get; set; }
    public int SoLuongHienTai { get; set; }
    public DateOnly? NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public string? LichHocTomTat { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? HinhAnh { get; set; }
    public bool IsOpenForRegistration { get; set; }
}

public sealed class CourseDetailDto
{
    public long CourseId { get; set; }
    public string MaKhoaHoc { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string LoaiBangLai { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal HocPhi { get; set; }
    public int SoBuoiHoc { get; set; }
    public int SoLuongToiDa { get; set; }
    public int SoLuongHienTai { get; set; }
    public DateOnly? NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public CourseTeacherDto? GiaoVienChinh { get; set; }
    public List<CourseScheduleDto> LichHocMau { get; set; } = new();
    public List<CourseClassDto> Classes { get; set; } = new();
    public string? HinhAnh { get; set; }
}

public sealed class CourseClassDto
{
    public long ClassId { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public int SiSoToiDa { get; set; }
    public int SoLuongHienTai { get; set; }
    public DateOnly? NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public bool IsOpenForRegistration { get; set; }
    public CourseTeacherDto? GiaoVien { get; set; }
    public List<CourseScheduleDto> LichHoc { get; set; } = new();
}

public sealed class CourseTeacherDto
{
    public long TeacherId { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
}

public sealed class CourseScheduleDto
{
    public int ThuTrongTuan { get; set; }
    public string GioBatDau { get; set; } = string.Empty;
    public string GioKetThuc { get; set; } = string.Empty;
    public string? DiaDiem { get; set; }
}

public sealed class CreateCourseRegistrationRequestDto
{
    public long CourseId { get; set; }
    public long ClassId { get; set; }
    public string? GhiChu { get; set; }
}

public sealed class CourseRegistrationDto
{
    public long RegistrationId { get; set; }
    public long StudentId { get; set; }
    public long CourseId { get; set; }
    public long ClassId { get; set; }
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public DateTime NgayDangKy { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public sealed class ApproveCourseRegistrationRequestDto
{
    public long ClassId { get; set; }
}

public sealed class MyCourseRegistrationDto
{
    public long RegistrationId { get; set; }
    public long CourseId { get; set; }
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string LoaiBangLai { get; set; } = string.Empty;
    public decimal HocPhi { get; set; }
    public DateTime NgayDangKy { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public long? ClassId { get; set; }
    public string? TenLop { get; set; }
    public DateOnly? NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
}
