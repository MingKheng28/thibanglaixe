namespace HeThongThiBangLai.Api.DTOs.Payments;

public sealed class CreateZaloPayOrderRequestDto
{
    public long RegistrationId { get; set; }

    /// <summary>
    /// Optional ZaloPay bank_code/payment channel selected by FE.
    /// Supported aliases: QR, ZALOPAYAPP, ATM, CC.
    /// Leave empty to let ZaloPay choose the default payment page.
    /// </summary>
    public string? PaymentMethod { get; set; }
}

public sealed class CreateZaloPayOrderResponseDto
{
    public long ReceiptId { get; set; }
    public string AppTransId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string OrderUrl { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public int ZaloPayReturnCode { get; set; }
    public string ZaloPayReturnMessage { get; set; } = string.Empty;
}

public sealed class ZaloPayPaymentStatusDto
{
    public long ReceiptId { get; set; }
    public string AppTransId { get; set; } = string.Empty;
    public long RegistrationId { get; set; }
    public long Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public sealed class ZaloPayCallbackResultDto
{
    public int ReturnCode { get; set; }
    public string ReturnMessage { get; set; } = string.Empty;
}
