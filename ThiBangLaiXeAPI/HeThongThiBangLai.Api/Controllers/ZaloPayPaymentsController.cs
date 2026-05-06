using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json.Serialization;
using HeThongThiBangLai.Api.Common.Responses;
using HeThongThiBangLai.Api.DTOs.Payments;
using HeThongThiBangLai.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HeThongThiBangLai.Api.Controllers;

[ApiController]
[Route("api/v1/payments/zalopay")]
public sealed class ZaloPayPaymentsController : ControllerBase
{
    private readonly IZaloPayPaymentService _zaloPayPaymentService;

    public ZaloPayPaymentsController(IZaloPayPaymentService zaloPayPaymentService)
    {
        _zaloPayPaymentService = zaloPayPaymentService;
    }

    [HttpPost("create-order")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CreateZaloPayOrderResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateZaloPayOrderRequestDto request)
    {
        var result = await _zaloPayPaymentService.CreateOrderAsync(request, GetCurrentUserId());
        return Ok(result);
    }

    [HttpGet("receipts/{receiptId:long}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ZaloPayPaymentStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(long receiptId)
    {
        var result = await _zaloPayPaymentService.GetStatusAsync(receiptId, GetCurrentUserId());
        return Ok(result);
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ZaloPayCallbackResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback()
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        var result = await _zaloPayPaymentService.HandleCallbackAsync(rawBody);
        return Ok(new ZaloPayCallbackResponse
        {
            ReturnCode = result.ReturnCode,
            ReturnMessage = result.ReturnMessage
        });
    }

    private long GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return long.TryParse(userIdValue, out var userId) ? userId : 0;
    }

    public sealed class ZaloPayCallbackResponse
    {
        [JsonPropertyName("return_code")]
        public int ReturnCode { get; set; }

        [JsonPropertyName("return_message")]
        public string ReturnMessage { get; set; } = string.Empty;
    }
}
