using DistributedConfigHub.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DistributedConfigHub.Api.Infrastructure.ExceptionHandling;

public class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Yalnızca bizim fırlattığımız Custom ValidationException ise yakalama işlemini yap
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var problemDetails = new ValidationProblemDetails(validationException.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred in the request data.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        
        // Hatayı ele aldık, akışı başka middleware'e sokmaya gerek yok dedik
        return true;
    }
}
