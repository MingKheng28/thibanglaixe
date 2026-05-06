using FluentValidation;
using HeThongThiBangLai.Api.DTOs.Auth;

namespace HeThongThiBangLai.Api.Validators.Auth;

public class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequestDto>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(x => x.ten_dang_nhap)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống")
            .MaximumLength(50).WithMessage("Tên đăng nhập không được vượt quá 50 ký tự")
            .When(x => x.ten_dang_nhap is not null);

        RuleFor(x => x.email)
            .EmailAddress().WithMessage("Email không hợp lệ")
            .When(x => x.email is not null);

        RuleFor(x => x.so_dien_thoai)
            .MaximumLength(20).WithMessage("Số điện thoại không được vượt quá 20 ký tự")
            .When(x => x.so_dien_thoai is not null);
    }
}
