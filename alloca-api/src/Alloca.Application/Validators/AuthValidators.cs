using Alloca.Application.DTOs.Auth;
using FluentValidation;

namespace Alloca.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName("E-mail");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithName("Senha");
    }
}

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName("E-mail");
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).WithName("Nome completo");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithName("Senha");
    }
}
