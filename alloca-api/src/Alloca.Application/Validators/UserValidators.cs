using Alloca.Application.DTOs.Users;
using Alloca.Domain.Enums;
using FluentValidation;

namespace Alloca.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(120).WithName("Nome completo");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(180).WithName("E-mail");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100).WithName("Senha");
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, ignoreCase: false, out _))
            .WithMessage("Perfil inválido. Use: Member, PavilionManager ou Admin.")
            .WithName("Perfil");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(120).WithName("Nome completo");
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => Enum.TryParse<UserRole>(r, ignoreCase: false, out _))
            .WithMessage("Perfil inválido. Use: Member, PavilionManager ou Admin.")
            .WithName("Perfil");
    }
}

public class ResetUserPasswordRequestValidator : AbstractValidator<ResetUserPasswordRequest>
{
    public ResetUserPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100).WithName("Nova senha");
    }
}
