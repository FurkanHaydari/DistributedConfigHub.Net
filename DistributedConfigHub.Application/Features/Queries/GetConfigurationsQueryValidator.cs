using FluentValidation;

namespace DistributedConfigHub.Application.Features.Queries;

public class GetConfigurationsQueryValidator : AbstractValidator<GetConfigurationsQuery>
{
    public GetConfigurationsQueryValidator()
    {
        RuleFor(x => x.ApplicationName)
            .NotEmpty().WithMessage("ApplicationName query parameter is required.");
    }
}
