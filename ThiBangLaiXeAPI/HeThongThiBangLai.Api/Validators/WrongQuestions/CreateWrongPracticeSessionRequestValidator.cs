using FluentValidation;
using HeThongThiBangLai.Api.DTOs.WrongQuestions;

namespace HeThongThiBangLai.Api.Validators.WrongQuestions;

public class CreateWrongPracticeSessionRequestValidator : AbstractValidator<CreateWrongPracticeSessionRequestDto>
{
    public CreateWrongPracticeSessionRequestValidator()
    {
        RuleFor(x => x.QuestionIds)
            .NotNull().WithMessage("QuestionIds is required")
            .Must(x => x.Count > 0).WithMessage("QuestionIds must contain at least one question")
            .Must(x => x.Count <= 20).WithMessage("QuestionIds cannot contain more than 20 questions");

        RuleForEach(x => x.QuestionIds)
            .GreaterThan(0).WithMessage("Each questionId must be greater than 0");
    }
}
