using FluentValidation;

namespace DistributedConfigHub.Application.Features.Queries;

public class GetConfigurationHistoryQueryValidator : AbstractValidator<GetConfigurationHistoryQuery>
{
    public GetConfigurationHistoryQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Configuration ID is required.");
    }
}
