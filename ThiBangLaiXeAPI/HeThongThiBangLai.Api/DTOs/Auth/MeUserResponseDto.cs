namespace HeThongThiBangLai.Api.DTOs.Auth;

public class MeUserResponseDto
{
    public long user_id { get; set; }
    public string ten_dang_nhap { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string? so_dien_thoai { get; set; }
    public string trang_thai { get; set; } = string.Empty;
    public List<string> roles { get; set; } = new();
}
