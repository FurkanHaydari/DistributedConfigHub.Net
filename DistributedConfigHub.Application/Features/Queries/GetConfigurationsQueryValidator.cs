using FluentValidation;

namespace DistributedConfigHub.Application.Features.Queries;

public class GetConfigurationsQueryValidator : AbstractValidator<GetConfigurationsQuery>
{
    public GetConfigurationsQueryValidator()
    {
        RuleFor(x => x.ApplicationName)
            .NotEmpty().WithMessage("ApplicationName query parameter is required.");

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage("Environment query parameter is required.");
    }
}
