using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Payments;

namespace HeThongThiBangLai.Api.Services.Interfaces;

public interface IZaloPayPaymentService
{
    Task<ApiResponse<CreateZaloPayOrderResponseDto>> CreateOrderAsync(CreateZaloPayOrderRequestDto request, long currentUserId);

    Task<ZaloPayCallbackResultDto> HandleCallbackAsync(string rawBody);

    Task<ApiResponse<ZaloPayPaymentStatusDto>> GetStatusAsync(long receiptId, long currentUserId);
}
