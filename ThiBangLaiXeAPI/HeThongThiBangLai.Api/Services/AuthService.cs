using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HeThongThiBangLai.Api.Common.Exceptions;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Auth;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Repositories.Interfaces;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace HeThongThiBangLai.Api.Services;

public class AuthService : IAuthService
{
    private const string DefaultHocVienRoleCode = "HOC_VIEN";
    private const string ForgotPasswordAction = "FORGOT_PASSWORD";

    private readonly IAuthRepository _authRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailSender _emailSender;
    private readonly PasswordHasher<nguoi_dung> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IAuthRepository authRepository,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _emailSender = emailSender;
        _configuration = configuration;
        _passwordHasher = new PasswordHasher<nguoi_dung>();
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request, string? ipAddress = null)
    {
        var username = request.ten_dang_nhap.Trim();
        var email = request.email.Trim().ToLowerInvariant();
        var phoneNumber = string.IsNullOrWhiteSpace(request.so_dien_thoai) ? null : request.so_dien_thoai.Trim();

        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Tên đăng nhập không hợp lệ.");

        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.mat_khau) || request.mat_khau.Length < 8)
            throw new InvalidOperationException("Mật khẩu phải có ít nhất 8 ký tự.");

        var conflictErrors = new List<ApiError>();

        var existedByUsername = await _authRepository.FindUserByUsernameAsync(username);
        if (existedByUsername is not null)
        {
            conflictErrors.Add(new ApiError
            {
                Code = "TRUNG_TEN_DANG_NHAP",
                Field = "ten_dang_nhap",
                Detail = "Tên đăng nhập đã tồn tại trong hệ thống."
            });
        }

        var existedByEmail = await _authRepository.FindUserByEmailAsync(email);
        if (existedByEmail is not null)
        {
            conflictErrors.Add(new ApiError
            {
                Code = "TRUNG_EMAIL",
                Field = "email",
                Detail = "Email đã tồn tại trong hệ thống."
            });
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var existedByPhoneNumber = await _authRepository.FindUserByPhoneNumberAsync(phoneNumber);
            if (existedByPhoneNumber is not null)
            {
                conflictErrors.Add(new ApiError
                {
                    Code = "TRUNG_SO_DIEN_THOAI",
                    Field = "so_dien_thoai",
                    Detail = "Số điện thoại đã tồn tại trong hệ thống."
                });
            }
        }

        if (conflictErrors.Count > 0)
            throw new ConflictAppException("Thông tin tài khoản đã tồn tại trong hệ thống.", "THONG_TIN_TAI_KHOAN_BI_TRUNG", conflictErrors);

        var now = DateTime.UtcNow;
        var user = new nguoi_dung
        {
            ten_dang_nhap = username,
            email = email,
            so_dien_thoai = phoneNumber,
            trang_thai = "hoat_dong",
            created_at = now,
            updated_at = now
        };
        user.mat_khau_hash = _passwordHasher.HashPassword(user, request.mat_khau);

        await _authRepository.AddUserAsync(user);
        await _authRepository.SaveChangesAsync();

        var defaultRole = await _authRepository.FindRoleByCodeAsync(DefaultHocVienRoleCode);
        if (defaultRole is null)
            throw new InvalidOperationException("Không tìm thấy role mặc định HOC_VIEN.");

        await _authRepository.AddUserRoleAsync(new nguoi_dung_vai_tro
        {
            nguoi_dung_id = user.id,
            vai_tro_id = defaultRole.id
        });

        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "REGISTER",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = $"Đăng ký người dùng {username} và gán role {DefaultHocVienRoleCode}",
            ip_address = ipAddress,
            created_at = now
        });

        await _authRepository.SaveChangesAsync();

        return new RegisterResponseDto
        {
            user_id = user.id,
            ten_dang_nhap = user.ten_dang_nhap,
            email = user.email,
            role_mac_dinh = DefaultHocVienRoleCode,
            created_at = user.created_at
        };
    }

    public async Task<MeResponseDto> RegisterStudentProfileAsync(long userId, RegisterStudentProfileRequestDto request, string? ipAddress = null)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var existedHocVien = await _authRepository.FindHocVienByUserIdAsync(userId);
        if (existedHocVien is not null)
        {
            throw new ConflictAppException(
                "Người dùng đã có hồ sơ học viên.",
                "HOC_VIEN_DA_TON_TAI",
                new List<ApiError>
                {
                    new()
                    {
                        Code = "HOC_VIEN_DA_TON_TAI",
                        Field = "hoc_vien",
                        Detail = "Người dùng đã có hồ sơ học viên."
                    }
                });
        }

        var cccd = string.IsNullOrWhiteSpace(request.cccd) ? null : request.cccd.Trim();
        if (!string.IsNullOrWhiteSpace(cccd))
        {
            var existedByCccd = await _authRepository.FindHocVienByCccdAsync(cccd);
            if (existedByCccd is not null)
            {
                throw new ConflictAppException(
                    "CCCD đã tồn tại trong hệ thống.",
                    "TRUNG_CCCD",
                    new List<ApiError>
                    {
                        new()
                        {
                            Code = "TRUNG_CCCD",
                            Field = "cccd",
                            Detail = "CCCD đã tồn tại trong hệ thống."
                        }
                    });
            }
        }

        var now = DateTime.UtcNow;
        var hocVien = new hoc_vien
        {
            nguoi_dung_id = user.id,
            ho_ten = request.ho_ten.Trim(),
            ngay_sinh = request.ngay_sinh,
            gioi_tinh = request.gioi_tinh,
            cccd = cccd,
            dia_chi = request.dia_chi,
            anh_chan_dung = request.anh_chan_dung,
            created_at = now
        };

        await _authRepository.AddHocVienProfileAsync(hocVien);
        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "REGISTER_STUDENT_PROFILE",
            bang_tac_dong = "hoc_vien",
            khoa_chinh_du_lieu = hocVien.id,
            noi_dung = $"Đăng ký hồ sơ học viên cho người dùng {user.ten_dang_nhap}",
            ip_address = ipAddress,
            created_at = now
        });

        await _authRepository.SaveChangesAsync();

        var roles = await _authRepository.GetRolesByUserIdAsync(user.id);
        return MapProfile(user, hocVien, roles.Select(x => x.ma_vai_tro).ToList());
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null)
    {
        var identity = request.ten_dang_nhap_hoac_email.Trim();
        if (string.IsNullOrWhiteSpace(identity) || string.IsNullOrWhiteSpace(request.mat_khau))
            throw new InvalidOperationException("Thông tin đăng nhập không hợp lệ.");

        nguoi_dung? user = identity.Contains('@')
            ? await _authRepository.FindUserByEmailAsync(identity.ToLowerInvariant())
            : await _authRepository.FindUserByUsernameAsync(identity);

        if (user is null)
            throw new UnauthorizedAccessException("Sai thông tin đăng nhập.");

        if (_passwordHasher.VerifyHashedPassword(user, user.mat_khau_hash, request.mat_khau) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Sai thông tin đăng nhập.");

        var roles = await _authRepository.GetRolesByUserIdAsync(user.id);

        user.lan_dang_nhap_cuoi = DateTime.UtcNow;
        user.updated_at = DateTime.UtcNow;
        await _authRepository.UpdateUserAsync(user);

        var jwtResult = _jwtTokenService.GenerateToken(new JwtTokenRequestDto
        {
            user_id = user.id,
            username = user.ten_dang_nhap,
            email = user.email,
            roles = roles.Select(x => x.ma_vai_tro).ToList()
        });

        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "LOGIN",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = "Đăng nhập thành công",
            ip_address = ipAddress,
            created_at = DateTime.UtcNow
        });

        await _authRepository.SaveChangesAsync();

        return new LoginResponseDto
        {
            user_id = user.id,
            ten_dang_nhap = user.ten_dang_nhap,
            email = user.email,
            access_token = jwtResult.token,
            expires_at_utc = jwtResult.expires_at_utc,
            roles = roles.Select(x => x.ma_vai_tro).ToList()
        };
    }

    public async Task LogoutAsync(long userId, string? ipAddress = null)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "LOGOUT",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = "Đăng xuất",
            ip_address = ipAddress,
            created_at = DateTime.UtcNow
        });

        await _authRepository.SaveChangesAsync();
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request, string? ipAddress = null)
    {
        var email = request.email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email không hợp lệ.");

        var user = await _authRepository.FindUserByEmailAsync(email);
        if (user is not null)
        {
            var rawToken = GenerateResetToken();
            var tokenHash = HashToken(rawToken);
            var expiresAt = DateTime.UtcNow.AddMinutes(GetResetTokenExpirationMinutes());

            var payload = new PasswordResetLogPayload
            {
                token_hash = tokenHash,
                expires_at_utc = expiresAt,
                used = false,
                email = user.email
            };

            await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
            {
                nguoi_dung_id = user.id,
                hanh_dong = ForgotPasswordAction,
                bang_tac_dong = "nguoi_dung",
                khoa_chinh_du_lieu = user.id,
                noi_dung = JsonSerializer.Serialize(payload),
                ip_address = ipAddress,
                created_at = DateTime.UtcNow
            });

            await _authRepository.SaveChangesAsync();

            var resetLink = BuildResetPasswordLink(user.email, rawToken);
            await _emailSender.SendAsync(
                user.email,
                "Yêu cầu đặt lại mật khẩu",
                $"<p>Bạn đã yêu cầu đặt lại mật khẩu.</p><p>Nhấn vào link sau để đặt lại mật khẩu:</p><p><a href=\"{resetLink}\">Đặt lại mật khẩu</a></p><p>Link có hiệu lực trong {GetResetTokenExpirationMinutes()} phút.</p>");
        }

        return new ForgotPasswordResponseDto
        {
            message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu sẽ được gửi."
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, string? ipAddress = null)
    {
        var email = request.email.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.reset_token))
            throw new InvalidOperationException("Reset token không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.mat_khau_moi) || request.mat_khau_moi.Length < 8)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        var user = await _authRepository.FindUserByEmailAsync(email)
            ?? throw new InvalidOperationException("Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

        var logs = await _authRepository.GetSystemLogsByUserAndActionAsync(user.id, ForgotPasswordAction);
        var requestTokenHash = HashToken(request.reset_token);
        var now = DateTime.UtcNow;

        nhat_ky_he_thong? matchedLog = null;
        PasswordResetLogPayload? matchedPayload = null;

        foreach (var log in logs.OrderByDescending(x => x.created_at))
        {
            if (!TryParseResetPayload(log.noi_dung, out var payload) || payload is null)
                continue;

            if (!string.Equals(payload.token_hash, requestTokenHash, StringComparison.OrdinalIgnoreCase))
                continue;

            if (payload.used)
                continue;

            if (payload.expires_at_utc <= now)
                continue;

            matchedLog = log;
            matchedPayload = payload;
            break;
        }

        if (matchedLog is null || matchedPayload is null)
            throw new InvalidOperationException("Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");

        user.mat_khau_hash = _passwordHasher.HashPassword(user, request.mat_khau_moi);
        user.updated_at = now;
        await _authRepository.UpdateUserAsync(user);

        matchedPayload.used = true;
        matchedPayload.used_at_utc = now;
        matchedLog.noi_dung = JsonSerializer.Serialize(matchedPayload);
        await _authRepository.UpdateSystemLogAsync(matchedLog);

        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "RESET_PASSWORD",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = "Đặt lại mật khẩu bằng forgot-password token",
            ip_address = ipAddress,
            created_at = now
        });

        await _authRepository.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequestDto request, string? ipAddress = null)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var verify = _passwordHasher.VerifyHashedPassword(user, user.mat_khau_hash, request.mat_khau_cu);
        if (verify == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Mật khẩu cũ không đúng.");

        if (string.IsNullOrWhiteSpace(request.mat_khau_moi) || request.mat_khau_moi.Length < 8)
            throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 8 ký tự.");

        user.mat_khau_hash = _passwordHasher.HashPassword(user, request.mat_khau_moi);
        user.updated_at = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);
        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "CHANGE_PASSWORD",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = "Đổi mật khẩu",
            ip_address = ipAddress,
            created_at = DateTime.UtcNow
        });

        await _authRepository.SaveChangesAsync();
    }

    public async Task<MeUserResponseDto> GetCurrentUserAsync(long userId)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var roles = await _authRepository.GetRolesByUserIdAsync(user.id);
        return MapUser(user, roles.Select(x => x.ma_vai_tro).ToList());
    }

    public async Task<MeStudentProfileResponseDto> GetCurrentStudentProfileAsync(long userId)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        var hocVien = await _authRepository.FindHocVienByUserIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ học viên.");

        return MapStudentProfile(user.id, hocVien);
    }

    public async Task<MeUserResponseDto> UpdateCurrentUserProfileAsync(long userId, UpdateMeRequestDto request, string? ipAddress = null)
    {
        var user = await _authRepository.FindUserByIdAsync(userId)
            ?? throw new KeyNotFoundException("Không tìm thấy người dùng.");

        if (!string.IsNullOrWhiteSpace(request.ten_dang_nhap))
        {
            var normalizedUsername = request.ten_dang_nhap.Trim();
            var existedByUsername = await _authRepository.FindUserByUsernameAsync(normalizedUsername);
            if (existedByUsername is not null && existedByUsername.id != user.id)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại.");
            user.ten_dang_nhap = normalizedUsername;
        }

        if (!string.IsNullOrWhiteSpace(request.email))
        {
            var normalizedEmail = request.email.Trim().ToLowerInvariant();
            var existedByEmail = await _authRepository.FindUserByEmailAsync(normalizedEmail);
            if (existedByEmail is not null && existedByEmail.id != user.id)
                throw new InvalidOperationException("Email đã tồn tại.");
            user.email = normalizedEmail;
        }

        if (request.so_dien_thoai is not null)
        {
            var normalizedPhoneNumber = string.IsNullOrWhiteSpace(request.so_dien_thoai)
                ? null
                : request.so_dien_thoai.Trim();

            if (!string.IsNullOrWhiteSpace(normalizedPhoneNumber))
            {
                var existedByPhoneNumber = await _authRepository.FindUserByPhoneNumberAsync(normalizedPhoneNumber);
                if (existedByPhoneNumber is not null && existedByPhoneNumber.id != user.id)
                    throw new InvalidOperationException("Số điện thoại đã tồn tại.");
            }

            user.so_dien_thoai = normalizedPhoneNumber;
        }

        user.updated_at = DateTime.UtcNow;

        await _authRepository.UpdateUserAsync(user);

        await _authRepository.AddSystemLogAsync(new nhat_ky_he_thong
        {
            nguoi_dung_id = user.id,
            hanh_dong = "UPDATE_PROFILE",
            bang_tac_dong = "nguoi_dung",
            khoa_chinh_du_lieu = user.id,
            noi_dung = "Cập nhật thông tin người dùng",
            ip_address = ipAddress,
            created_at = DateTime.UtcNow
        });

        await _authRepository.SaveChangesAsync();

        var roles = await _authRepository.GetRolesByUserIdAsync(user.id);
        return MapUser(user, roles.Select(x => x.ma_vai_tro).ToList());
    }

    private static MeUserResponseDto MapUser(nguoi_dung user, List<string> roles)
    {
        return new MeUserResponseDto
        {
            user_id = user.id,
            ten_dang_nhap = user.ten_dang_nhap,
            email = user.email,
            so_dien_thoai = user.so_dien_thoai,
            trang_thai = user.trang_thai,
            roles = roles
        };
    }

    private static MeStudentProfileResponseDto MapStudentProfile(long userId, hoc_vien hocVien)
    {
        return new MeStudentProfileResponseDto
        {
            hoc_vien_id = hocVien.id,
            user_id = userId,
            ho_ten = hocVien.ho_ten,
            ngay_sinh = hocVien.ngay_sinh,
            gioi_tinh = hocVien.gioi_tinh,
            cccd = hocVien.cccd,
            dia_chi = hocVien.dia_chi,
            anh_chan_dung = hocVien.anh_chan_dung
        };
    }

    private MeResponseDto MapProfile(nguoi_dung user, hoc_vien hocVien, List<string> roles)
    {
        return new MeResponseDto
        {
            user_id = user.id,
            ten_dang_nhap = user.ten_dang_nhap,
            email = user.email,
            so_dien_thoai = user.so_dien_thoai,
            trang_thai = user.trang_thai,
            hoc_vien_id = hocVien.id,
            ho_ten = hocVien.ho_ten,
            ngay_sinh = hocVien.ngay_sinh,
            gioi_tinh = hocVien.gioi_tinh,
            cccd = hocVien.cccd,
            dia_chi = hocVien.dia_chi,
            anh_chan_dung = hocVien.anh_chan_dung,
            roles = roles
        };
    }

    private int GetResetTokenExpirationMinutes()
    {
        return int.TryParse(_configuration["ResetPassword:ExpirationMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 30;
    }

    private string BuildResetPasswordLink(string email, string rawToken)
    {
        var baseUrl = _configuration["ResetPassword:FrontendResetUrl"] ?? "https://example.com/reset-password";
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(rawToken);
        return $"{baseUrl}?email={encodedEmail}&token={encodedToken}";
    }

    private static string GenerateResetToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private static bool TryParseResetPayload(string? json, out PasswordResetLogPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            payload = JsonSerializer.Deserialize<PasswordResetLogPayload>(json);
            return payload is not null;
        }
        catch
        {
            return false;
        }
    }

    private sealed class PasswordResetLogPayload
    {
        public string token_hash { get; set; } = string.Empty;
        public DateTime expires_at_utc { get; set; }
        public bool used { get; set; }
        public DateTime? used_at_utc { get; set; }
        public string email { get; set; } = string.Empty;
    }
}
