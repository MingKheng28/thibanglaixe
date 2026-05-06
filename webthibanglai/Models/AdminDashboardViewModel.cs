using System.Text.Json;
using System.Text.Json.Serialization;

namespace webthibanglai.Models;

public sealed class AdminDashboardViewModel
{
    public string? ErrorMessage { get; set; }
    public string AdminName { get; set; } = "Admin";
    public string AdminEmail { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalCourses { get; set; }
    public int OpenCourses { get; set; }
    public int TotalClasses { get; set; }
    public int TotalSchedules { get; set; }
    public int TotalQuestions { get; set; }
    public int CriticalQuestions { get; set; }
    public int TotalExams { get; set; }
    public int TotalReceipts { get; set; }
    public decimal TotalReceiptAmount { get; set; }
    public int PendingReceipts { get; set; }
    public List<AdminUserItem> RecentUsers { get; set; } = new();
    public List<AdminCourseItem> Courses { get; set; } = new();
    public List<AdminScheduleItem> TodaySchedules { get; set; } = new();
    public List<AdminExamItem> RecentExams { get; set; } = new();
    public List<AdminReceiptItem> RecentReceipts { get; set; } = new();
}

public sealed class AdminUserItem
{
    public long Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public List<AdminRoleItem> Roles { get; set; } = new();
}

public sealed class AdminRoleItem
{
    public long Id { get; set; }
    public string MaVaiTro { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
}

public sealed class AdminCourseItem
{
    public long Id { get; set; }
    public string MaKhoaHoc { get; set; } = string.Empty;
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal HocPhi { get; set; }
    public int? ThoiLuong { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class AdminClassItem
{
    public long Id { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public long KhoaHocId { get; set; }
    public DateOnly? NgayBatDau { get; set; }
    public DateOnly? NgayKetThuc { get; set; }
    public int SiSoToiDa { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class AdminScheduleItem
{
    public long Id { get; set; }
    public long LopHocId { get; set; }
    public string TenBuoi { get; set; } = string.Empty;
    public DateOnly NgayHoc { get; set; }
    public TimeOnly GioBatDau { get; set; }
    public TimeOnly GioKetThuc { get; set; }
    public string? NoiDung { get; set; }
    public string? PhongHoc { get; set; }
}

public sealed class AdminQuestionItem
{
    public long Id { get; set; }
    public long ChuDeId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string? GiaiThichDapAn { get; set; }
    public string? LoaiCauHoi { get; set; }
    public string? MucDo { get; set; }
    public bool LaCauDiemLiet { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class AdminExamItem
{
    public long Id { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public int TongSoCau { get; set; }
    public int ThoiGianLamBai { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? LoaiDeThi { get; set; }
    public DateTime? NgayTao { get; set; }
}

public sealed class AdminReceiptItem
{
    public long Id { get; set; }
    public string MaPhieuThu { get; set; } = string.Empty;
    public decimal TongTien { get; set; }
    public DateTime? NgayThu { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public sealed class AdminApiEnvelope<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
