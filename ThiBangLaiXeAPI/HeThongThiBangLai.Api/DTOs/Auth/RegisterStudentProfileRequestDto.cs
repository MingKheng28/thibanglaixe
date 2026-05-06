namespace HeThongThiBangLai.Api.DTOs.Auth;

public class RegisterStudentProfileRequestDto
{
    public string ho_ten { get; set; } = string.Empty;
    public DateOnly? ngay_sinh { get; set; }
    public string? gioi_tinh { get; set; }
    public string? cccd { get; set; }
    public string? dia_chi { get; set; }
    public string? anh_chan_dung { get; set; }
}
