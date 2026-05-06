namespace HeThongThiBangLai.Api.DTOs.Questions;

public sealed class QuestionWithAnswersDto
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public string TopicCode { get; set; } = string.Empty;
    public string TopicName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? Level { get; set; }
    public bool IsCritical { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string? ImageUrl { get; set; }
    public List<QuestionAnswerOptionDto> Answers { get; set; } = [];
}

public sealed class QuestionAnswerOptionDto
{
    public long AnswerId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool? IsCorrect { get; set; }
}
