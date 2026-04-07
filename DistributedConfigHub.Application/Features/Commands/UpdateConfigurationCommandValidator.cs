using FluentValidation;

namespace DistributedConfigHub.Application.Features.Commands;

public class UpdateConfigurationCommandValidator : AbstractValidator<UpdateConfigurationCommand>
{
    public UpdateConfigurationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(500).WithMessage("Value cannot exceed 500 characters.");
    }
}
