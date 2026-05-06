using FluentValidation;
using HeThongThiBangLai.Api.DTOs.Courses;

namespace HeThongThiBangLai.Api.Validators.Courses;

public sealed class ApproveCourseRegistrationRequestValidator : AbstractValidator<ApproveCourseRegistrationRequestDto>
{
    public ApproveCourseRegistrationRequestValidator()
    {
        RuleFor(x => x.ClassId)
            .GreaterThan(0).WithMessage("ClassId must be greater than 0");
    }
}
