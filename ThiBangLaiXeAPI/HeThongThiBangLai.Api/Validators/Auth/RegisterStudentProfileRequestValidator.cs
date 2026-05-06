using FluentValidation;
using HeThongThiBangLai.Api.DTOs.Auth;

namespace HeThongThiBangLai.Api.Validators.Auth;

public class RegisterStudentProfileRequestValidator : AbstractValidator<RegisterStudentProfileRequestDto>
{
    public RegisterStudentProfileRequestValidator()
    {
        RuleFor(x => x.ho_ten)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(150).WithMessage("Họ tên không được vượt quá 150 ký tự");

        RuleFor(x => x.gioi_tinh)
            .MaximumLength(10).WithMessage("Giới tính không được vượt quá 10 ký tự");

        RuleFor(x => x.cccd)
            .MaximumLength(20).WithMessage("CCCD không được vượt quá 20 ký tự");

        RuleFor(x => x.dia_chi)
            .MaximumLength(255).WithMessage("Địa chỉ không được vượt quá 255 ký tự");

        RuleFor(x => x.anh_chan_dung)
            .MaximumLength(255).WithMessage("Ảnh chân dung không được vượt quá 255 ký tự");
    }
}
