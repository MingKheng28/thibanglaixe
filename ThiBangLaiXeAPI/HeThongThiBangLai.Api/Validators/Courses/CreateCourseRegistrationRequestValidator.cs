using FluentValidation;
using HeThongThiBangLai.Api.DTOs.Courses;

namespace HeThongThiBangLai.Api.Validators.Courses;

public sealed class CreateCourseRegistrationRequestValidator : AbstractValidator<CreateCourseRegistrationRequestDto>
{
    public CreateCourseRegistrationRequestValidator()
    {
        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("CourseId must be greater than 0");

        RuleFor(x => x.ClassId)
            .GreaterThan(0).WithMessage("ClassId must be greater than 0");
    }
}
