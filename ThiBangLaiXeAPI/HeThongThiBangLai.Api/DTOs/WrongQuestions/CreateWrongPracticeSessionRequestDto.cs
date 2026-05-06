namespace HeThongThiBangLai.Api.DTOs.WrongQuestions;

public class CreateWrongPracticeSessionRequestDto
{
    public List<long> QuestionIds { get; set; } = new();
}
