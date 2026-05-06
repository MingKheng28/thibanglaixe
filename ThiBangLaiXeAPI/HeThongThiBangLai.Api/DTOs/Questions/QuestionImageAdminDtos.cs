namespace HeThongThiBangLai.Api.DTOs.Questions;

public sealed class QuestionImageAdminListItemDto
{
    public long QuestionId { get; set; }
    public string TopicCode { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public long? FileId { get; set; }
}

public sealed class QuestionImageUploadResponseDto
{
    public long QuestionId { get; set; }
    public long FileId { get; set; }
    public long FileUsageId { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
