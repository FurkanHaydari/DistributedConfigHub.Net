using FluentValidation;

namespace DistributedConfigHub.Application.Features.Commands;

public class CreateConfigurationCommandValidator : AbstractValidator<CreateConfigurationCommand>
{
    public CreateConfigurationCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid ConfigurationType (0 = Int, 1 = String, 2 = Boolean, 3 = Double).");

        RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Value is required.")
            .MaximumLength(500).WithMessage("Value cannot exceed 500 characters.");

        RuleFor(x => x.ApplicationName)
            .NotEmpty().WithMessage("ApplicationName is required.")
            .MaximumLength(150).WithMessage("ApplicationName cannot exceed 150 characters.");

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage("Environment is required.")
            .MaximumLength(50).WithMessage("Environment cannot exceed 50 characters.");
    }
}
