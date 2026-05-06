using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using HeThongThiBangLai.Api.Common.Exceptions;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.Data;
using HeThongThiBangLai.Api.DTOs.Payments;
using HeThongThiBangLai.Api.Models;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HeThongThiBangLai.Api.Services.Payments;

public sealed class ZaloPayOptions
{
    public int AppId { get; set; } = 2553;
    public string Key1 { get; set; } = "sbkey";
    public string Key2 { get; set; } = "sbkey";
    public string CreateOrderEndpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2/create";
    public string CallbackUrl { get; set; } = "https://localhost:5001/api/v1/payments/zalopay/callback";
    public string RedirectUrl { get; set; } = "http://localhost:3000/payments/zalopay/result";
    public int ExpireDurationSeconds { get; set; } = 900;
}

public sealed class ZaloPayPaymentService : IZaloPayPaymentService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ZaloPaySandboxGatewayUrl = "https://sandbox.zalopay.com.vn/pay?order=";

    private readonly ApplicationDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly ZaloPayOptions _options;

    public ZaloPayPaymentService(ApplicationDbContext dbContext, HttpClient httpClient, IOptions<ZaloPayOptions> options)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ApiResponse<CreateZaloPayOrderResponseDto>> CreateOrderAsync(CreateZaloPayOrderRequestDto request, long currentUserId)
    {
        var student = await _dbContext.hoc_viens.FirstOrDefaultAsync(item => item.nguoi_dung_id == currentUserId);
        if (student is null)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ học viên của tài khoản hiện tại");
        }

        var registration = await _dbContext.dang_ky_khoa_hocs
            .Include(item => item.khoa_hoc)
            .FirstOrDefaultAsync(item => item.id == request.RegistrationId && item.hoc_vien_id == student.id);
        if (registration is null)
        {
            throw new NotFoundAppException("Không tìm thấy đăng ký khóa học");
        }

        if (!string.Equals(registration.trang_thai, "da_duyet", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleAppException("Chỉ có thể thanh toán đăng ký đã được duyệt", "REGISTRATION_NOT_APPROVED");
        }

        var existedPaidReceipt = await _dbContext.phieu_thus.AnyAsync(receipt =>
            receipt.hoc_vien_id == student.id
            && receipt.trang_thai == "da_xac_nhan"
            && receipt.chi_tiet_phieu_thus.Any(detail => detail.ghi_chu != null && detail.ghi_chu.Contains($"DKKH:{registration.id}")));
        if (existedPaidReceipt)
        {
            throw new ConflictAppException("Đăng ký khóa học này đã được thanh toán", "REGISTRATION_ALREADY_PAID");
        }

        var amount = decimal.ToInt64(decimal.Round(registration.khoa_hoc.hoc_phi, 0, MidpointRounding.AwayFromZero));
        if (amount <= 0)
        {
            throw new BusinessRuleAppException("Học phí không hợp lệ", "INVALID_COURSE_FEE");
        }

        var receipt = await GetOrCreatePendingReceiptAsync(student.id, registration, amount, currentUserId);
        var appTransId = receipt.ma_phieu_thu;
        var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var appUser = $"hoc_vien_{student.id}";
        var paymentChannel = ResolvePaymentChannel(request.PaymentMethod);
        var embedData = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["redirecturl"] = _options.RedirectUrl,
            ["registrationId"] = registration.id,
            ["receiptId"] = receipt.id,
            ["bankgroup"] = paymentChannel.BankGroup
        }.Where(item => item.Value is not null).ToDictionary(item => item.Key, item => item.Value), JsonOptions);
        var item = JsonSerializer.Serialize(new[]
        {
            new
            {
                itemid = registration.khoa_hoc.id,
                itemname = registration.khoa_hoc.ten_khoa_hoc,
                itemprice = amount,
                itemquantity = 1
            }
        }, JsonOptions);
        var description = $"Thanh toán học phí {registration.khoa_hoc.ten_khoa_hoc}";
        var bankCode = paymentChannel.BankCode;
        var macData = $"{_options.AppId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}";
        var mac = ComputeHmacSha256(macData, _options.Key1);

        var form = new Dictionary<string, string>
        {
            ["app_id"] = _options.AppId.ToString(),
            ["app_user"] = appUser,
            ["app_time"] = appTime.ToString(),
            ["amount"] = amount.ToString(),
            ["app_trans_id"] = appTransId,
            ["embed_data"] = embedData,
            ["item"] = item,
            ["description"] = description,
            ["bank_code"] = bankCode,
            ["callback_url"] = _options.CallbackUrl,
            ["mac"] = mac,
            ["expire_duration_seconds"] = _options.ExpireDurationSeconds.ToString()
        };

        using var response = await _httpClient.PostAsync(_options.CreateOrderEndpoint, new FormUrlEncodedContent(form));
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new BusinessRuleAppException($"ZaloPay create order failed: {responseBody}", "ZALOPAY_CREATE_ORDER_FAILED");
        }

        var zaloPayResponse = JsonSerializer.Deserialize<ZaloPayCreateOrderResponse>(responseBody, JsonOptions)
            ?? throw new BusinessRuleAppException("Không đọc được phản hồi ZaloPay", "ZALOPAY_INVALID_RESPONSE");

        var orderUrl = ResolveZaloPayOrderUrl(zaloPayResponse);
        if (string.IsNullOrWhiteSpace(orderUrl))
        {
            throw new BusinessRuleAppException(
                $"ZaloPay không trả về liên kết thanh toán hợp lệ. ReturnCode={zaloPayResponse.ReturnCode}, SubReturnCode={zaloPayResponse.SubReturnCode}, Message={zaloPayResponse.ReturnMessage}, SubMessage={zaloPayResponse.SubReturnMessage}",
                "ZALOPAY_INVALID_ORDER_URL");
        }

        return ApiResponseFactory.Success(new CreateZaloPayOrderResponseDto
        {
            ReceiptId = receipt.id,
            AppTransId = appTransId,
            Amount = amount,
            OrderUrl = orderUrl,
            PaymentStatus = ToApiPaymentStatus(receipt.trang_thai),
            ZaloPayReturnCode = zaloPayResponse.ReturnCode,
            ZaloPayReturnMessage = zaloPayResponse.ReturnMessage
        }, "Tạo đơn thanh toán ZaloPay thành công");
    }

    public async Task<ZaloPayCallbackResultDto> HandleCallbackAsync(string rawBody)
    {
        try
        {
            var callback = JsonSerializer.Deserialize<ZaloPayCallbackRequest>(rawBody, JsonOptions);
            if (callback is null || string.IsNullOrWhiteSpace(callback.Data) || string.IsNullOrWhiteSpace(callback.Mac))
            {
                return new ZaloPayCallbackResultDto { ReturnCode = -1, ReturnMessage = "Invalid callback payload" };
            }

            var computedMac = ComputeHmacSha256(callback.Data, _options.Key2);
            if (!string.Equals(computedMac, callback.Mac, StringComparison.OrdinalIgnoreCase))
            {
                return new ZaloPayCallbackResultDto { ReturnCode = -1, ReturnMessage = "Invalid mac" };
            }

            var data = JsonSerializer.Deserialize<ZaloPayCallbackData>(callback.Data, JsonOptions);
            if (data is null || string.IsNullOrWhiteSpace(data.AppTransId))
            {
                return new ZaloPayCallbackResultDto { ReturnCode = -1, ReturnMessage = "Invalid callback data" };
            }

            var receipt = await _dbContext.phieu_thus.FirstOrDefaultAsync(item => item.ma_phieu_thu == data.AppTransId);
            if (receipt is null)
            {
                return new ZaloPayCallbackResultDto { ReturnCode = -1, ReturnMessage = "Receipt not found" };
            }

            if (!string.Equals(receipt.trang_thai, "da_xac_nhan", StringComparison.OrdinalIgnoreCase))
            {
                receipt.trang_thai = "da_xac_nhan";
                receipt.nguoi_xac_nhan_id = null;
                await _dbContext.SaveChangesAsync();
            }

            return new ZaloPayCallbackResultDto { ReturnCode = 1, ReturnMessage = "success" };
        }
        catch
        {
            return new ZaloPayCallbackResultDto { ReturnCode = 0, ReturnMessage = "callback processing failed" };
        }
    }

    public async Task<ApiResponse<ZaloPayPaymentStatusDto>> GetStatusAsync(long receiptId, long currentUserId)
    {
        var student = await _dbContext.hoc_viens.AsNoTracking().FirstOrDefaultAsync(item => item.nguoi_dung_id == currentUserId);
        if (student is null)
        {
            throw new NotFoundAppException("Không tìm thấy hồ sơ học viên của tài khoản hiện tại");
        }

        var receipt = await _dbContext.phieu_thus
            .AsNoTracking()
            .Include(item => item.chi_tiet_phieu_thus)
            .FirstOrDefaultAsync(item => item.id == receiptId && item.hoc_vien_id == student.id);
        if (receipt is null)
        {
            throw new NotFoundAppException("Không tìm thấy phiếu thu");
        }

        var registrationId = ExtractRegistrationId(receipt.chi_tiet_phieu_thus.Select(item => item.ghi_chu).FirstOrDefault(item => item?.Contains("DKKH:") == true));

        return ApiResponseFactory.Success(new ZaloPayPaymentStatusDto
        {
            ReceiptId = receipt.id,
            AppTransId = receipt.ma_phieu_thu,
            RegistrationId = registrationId,
            Amount = decimal.ToInt64(decimal.Round(receipt.tong_tien, 0, MidpointRounding.AwayFromZero)),
            PaymentStatus = ToApiPaymentStatus(receipt.trang_thai)
        }, "Lấy trạng thái thanh toán thành công");
    }

    private async Task<phieu_thu> GetOrCreatePendingReceiptAsync(long studentId, dang_ky_khoa_hoc registration, long amount, long currentUserId)
    {
        var existedReceipt = await _dbContext.phieu_thus
            .Include(item => item.chi_tiet_phieu_thus)
            .FirstOrDefaultAsync(receipt =>
                receipt.hoc_vien_id == studentId
                && receipt.trang_thai == "cho_xac_nhan"
                && receipt.chi_tiet_phieu_thus.Any(detail => detail.ghi_chu != null && detail.ghi_chu.Contains($"DKKH:{registration.id}")));
        if (existedReceipt is not null)
        {
            // ZaloPay yêu cầu app_trans_id là duy nhất cho mỗi lần gọi Create Order.
            // Vẫn giữ một phiếu thu pending cho cùng đăng ký, nhưng rotate ma_phieu_thu
            // trước khi gửi lại ZaloPay để tránh lỗi SubReturnCode=-68 / trùng mã giao dịch.
            existedReceipt.ma_phieu_thu = GenerateAppTransId();
            existedReceipt.ngay_thu = DateTime.UtcNow;
            existedReceipt.tong_tien = amount;
            existedReceipt.nguoi_lap_id = currentUserId;

            foreach (var detail in existedReceipt.chi_tiet_phieu_thus)
            {
                detail.so_tien = amount;
            }

            await _dbContext.SaveChangesAsync();
            return existedReceipt;
        }

        var feeType = await _dbContext.loai_khoan_thus
            .OrderByDescending(item => item.ma_loai == "HOC_PHI")
            .ThenBy(item => item.id)
            .FirstOrDefaultAsync();
        if (feeType is null)
        {
            throw new BusinessRuleAppException("Chưa cấu hình loại khoản thu học phí", "FEE_TYPE_NOT_CONFIGURED");
        }

        var receipt = new phieu_thu
        {
            ma_phieu_thu = GenerateAppTransId(),
            hoc_vien_id = studentId,
            ngay_thu = DateTime.UtcNow,
            tong_tien = amount,
            trang_thai = "cho_xac_nhan",
            nguoi_lap_id = currentUserId,
            chi_tiet_phieu_thus = new List<chi_tiet_phieu_thu>
            {
                new()
                {
                    loai_khoan_thu_id = feeType.id,
                    so_tien = amount,
                    ghi_chu = $"DKKH:{registration.id};KHOA_HOC:{registration.khoa_hoc_id};ZALOPAY_SANDBOX"
                }
            }
        };

        _dbContext.phieu_thus.Add(receipt);
        await _dbContext.SaveChangesAsync();
        return receipt;
    }

    private static string GenerateAppTransId()
    {
        return $"{DateTime.UtcNow:yyMMdd}_{Guid.NewGuid():N}"[..30];
    }

    private static ZaloPayPaymentChannel ResolvePaymentChannel(string? paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
        {
            return new ZaloPayPaymentChannel(string.Empty, null);
        }

        return paymentMethod.Trim().ToUpperInvariant() switch
        {
            "DEFAULT" or "AUTO" => new ZaloPayPaymentChannel(string.Empty, null),
            "QR" or "ZALO_PAY" or "ZALOPAY" or "ZALOPAYAPP" => new ZaloPayPaymentChannel("zalopayapp", null),
            // Theo tài liệu Gateway v1: nếu muốn hiện nhóm ATM thì để bankcode rỗng và truyền bankgroup=ATM trong embeddata.
            "ATM" or "NAPAS" or "BANK" or "BANK_CARD" => new ZaloPayPaymentChannel(string.Empty, "ATM"),
            "CC" or "CREDIT" or "CREDIT_CARD" or "VISA" or "MASTERCARD" => new ZaloPayPaymentChannel("CC", null),
            _ => throw new BusinessRuleAppException(
                "Phương thức thanh toán ZaloPay không được hỗ trợ. Chỉ hỗ trợ: QR, ZALOPAYAPP, ATM, CC.",
                "ZALOPAY_PAYMENT_METHOD_NOT_SUPPORTED")
        };
    }

    private sealed record ZaloPayPaymentChannel(string BankCode, string? BankGroup);

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static long ExtractRegistrationId(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return 0;
        }

        var markerIndex = note.IndexOf("DKKH:", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return 0;
        }

        var start = markerIndex + "DKKH:".Length;
        var end = note.IndexOf(';', start);
        var value = end < 0 ? note[start..] : note[start..end];
        return long.TryParse(value, out var registrationId) ? registrationId : 0;
    }

    private static string ToApiPaymentStatus(string status)
    {
        return status switch
        {
            "da_xac_nhan" => "DaThanhToan",
            "cho_xac_nhan" => "ChoXacNhan",
            "da_huy" => "DaHuy",
            _ => status
        };
    }

    private static string ResolveZaloPayOrderUrl(ZaloPayCreateOrderResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.OrderUrl))
        {
            return response.OrderUrl;
        }

        if (!string.IsNullOrWhiteSpace(response.OrderToken))
        {
            return $"{ZaloPaySandboxGatewayUrl}{Uri.EscapeDataString(response.OrderToken)}";
        }

        return string.Empty;
    }

    private sealed class ZaloPayCreateOrderResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; set; } = string.Empty;

        [JsonPropertyName("sub_return_code")]
        public int SubReturnCode { get; set; }

        [JsonPropertyName("sub_return_message")]
        public string SubReturnMessage { get; set; } = string.Empty;

        [JsonPropertyName("order_url")]
        public string OrderUrl { get; set; } = string.Empty;

        [JsonPropertyName("order_token")]
        public string OrderToken { get; set; } = string.Empty;
    }

    private sealed class ZaloPayCallbackRequest
    {
        public string Data { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public int Type { get; set; }
    }

    private sealed class ZaloPayCallbackData
    {
        public string AppTransId { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long ZpTransId { get; set; }
    }
}
