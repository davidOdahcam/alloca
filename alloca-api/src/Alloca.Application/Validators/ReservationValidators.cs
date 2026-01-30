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
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
    }
}

public class CheckAvailabilityRequestValidator : AbstractValidator<CheckAvailabilityRequest>
{
    public CheckAvailabilityRequestValidator()
        => RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
}

public class ReasonRequestValidator : AbstractValidator<ReasonRequest>
{
    public ReasonRequestValidator()
        => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}

public class CreateBlockRequestValidator : AbstractValidator<CreateBlockRequest>
{
    public CreateBlockRequestValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StartUtc).LessThan(x => x.EndUtc);
    }
}
