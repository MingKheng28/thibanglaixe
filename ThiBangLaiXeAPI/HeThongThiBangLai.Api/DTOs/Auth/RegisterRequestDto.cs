namespace HeThongThiBangLai.Api.DTOs.Auth;

public class RegisterRequestDto
{
    public string ten_dang_nhap { get; set; } = string.Empty;
    public string mat_khau { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string? so_dien_thoai { get; set; }
}
