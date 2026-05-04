using FluentValidation;
using MediatR;
using ValidationException = DistributedConfigHub.Application.Exceptions.ValidationException;

namespace DistributedConfigHub.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Count > 0)
            {
                // We wrap FluentValidation errors into our own format so that 
                // we can standardize them on the API side
                throw new ValidationException(failures);
            }
        }

        // If the rule passes successfully or there are no rules, forward the command to the Handler (Next)
        return await next();
    }
}
