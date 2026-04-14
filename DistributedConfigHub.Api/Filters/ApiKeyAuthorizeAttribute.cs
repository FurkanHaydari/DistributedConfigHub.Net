using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace DistributedConfigHub.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiKeyAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "API Key is missing. Access denied." });
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var apiKeysSection = configuration.GetSection("ApiKeys");
        var matchingApp = apiKeysSection.GetChildren()
            .FirstOrDefault(child => string.Equals(child.Value, extractedApiKey, StringComparison.Ordinal));

        if (matchingApp == null)
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Invalid API Key. Access denied." });
            return;
        }
            
        context.HttpContext.Items["CallerApplicationName"] = matchingApp.Key; 

        await next();
    }
}