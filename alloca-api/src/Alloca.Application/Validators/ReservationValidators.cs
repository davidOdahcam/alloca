using Alloca.Application.DTOs.Availability;
using Alloca.Application.DTOs.Blocks;
using Alloca.Application.DTOs.Manager;
using Alloca.Application.DTOs.Reservations;
using FluentValidation;

namespace Alloca.Application.Validators;

public class CreateReservationRequestValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty().WithMessage("Selecione um recurso para reservar.");
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc).WithMessage("A data/hora de início deve ser anterior à de término.");
    }
}

public class CheckAvailabilityRequestValidator : AbstractValidator<CheckAvailabilityRequest>
{
    public CheckAvailabilityRequestValidator()
        => RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc).WithMessage("A data/hora de início deve ser anterior à de término.");
}

public class ReasonRequestValidator : AbstractValidator<ReasonRequest>
{
    public ReasonRequestValidator()
        => RuleFor(x => x.Reason).NotEmpty().WithMessage("Informe o motivo.").MaximumLength(500).WithMessage("O motivo deve ter no máximo 500 caracteres.");
}

public class CreateBlockRequestValidator : AbstractValidator<CreateBlockRequest>
{
    public CreateBlockRequestValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty().WithMessage("Selecione o recurso a ser bloqueado.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Informe o motivo do bloqueio.").MaximumLength(500).WithMessage("O motivo deve ter no máximo 500 caracteres.");
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc).WithMessage("A data/hora de início deve ser anterior à de término.");
    }
}
