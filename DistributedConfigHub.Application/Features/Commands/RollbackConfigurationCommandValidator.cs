using FluentValidation;

namespace DistributedConfigHub.Application.Features.Commands;

public class RollbackConfigurationCommandValidator : AbstractValidator<RollbackConfigurationCommand>
{
    public RollbackConfigurationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Configuration ID is required.");
            
        RuleFor(x => x.AuditLogId)
            .NotEmpty().WithMessage("Audit Log ID is required.");
    }
}
