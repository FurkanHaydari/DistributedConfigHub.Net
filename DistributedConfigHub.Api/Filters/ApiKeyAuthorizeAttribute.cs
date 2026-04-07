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
        // 1. Gelen HTTP İsteğinde "X-Api-Key" header'ı var mı?
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "API Key is missing. Access denied." });
            return;
        }

        // 2. QueryString veya Route içerisinde "applicationName" var mı?
        // Bu izolasyonun kilit noktasıdır
        var applicationName = context.HttpContext.Request.Query["applicationName"].ToString();
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            context.Result = new BadRequestObjectResult(new { Message = "applicationName parameter is required when accessing secure endpoints." });
            return;
        }

        // 3. appsettings.json içerisindeki "ApiKeys" objesine gidip o uygulama adına atanmış anahtarı al
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var validKeyForApplication = configuration.GetValue<string>($"ApiKeys:{applicationName}");

        if (string.IsNullOrWhiteSpace(validKeyForApplication))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = $"No configured API Key found for application: {applicationName}" });
            return;
        }

        // 4. İstemciden gelen anahtar ile o uygulamanın anahtarı eşleşiyor mu? (İzolasyon Kararı)
        if (!string.Equals(extractedApiKey, validKeyForApplication, StringComparison.Ordinal))
        {
            // Eşleşmiyorsa, bir uygulama BAŞKA BİR uygulamanın bilgilerine erişmeye çalışıyor demektir! (Yasak)
            context.Result = new StatusCodeResult(403); // Forbidden
            return;
        }

        // Yetkilendirme başarılı
        await next();
    }
}
