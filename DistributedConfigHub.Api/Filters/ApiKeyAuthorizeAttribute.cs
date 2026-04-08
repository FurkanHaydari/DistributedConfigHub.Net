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

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // 2. QueryString içerisinde "applicationName" var mı? (GET isteklerinde izolasyon kontrolü)
        var applicationName = context.HttpContext.Request.Query["applicationName"].ToString();

        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            // Tam izolasyon modu: Belirtilen uygulamanın anahtarı ile istemcinin anahtarı eşleşmeli
            var validKeyForApplication = configuration.GetValue<string>($"ApiKeys:{applicationName}");

            if (string.IsNullOrWhiteSpace(validKeyForApplication))
            {
                context.Result = new UnauthorizedObjectResult(new { Message = $"No configured API Key found for application: {applicationName}" });
                return;
            }

            if (!string.Equals(extractedApiKey, validKeyForApplication, StringComparison.Ordinal))
            {
                // Eşleşmiyorsa, bir uygulama BAŞKA BİR uygulamanın bilgilerine erişmeye çalışıyor demektir!
                context.Result = new StatusCodeResult(403); // Forbidden
                return;
            }
        }
        else
        {
            // Temel kimlik doğrulama modu: POST/PUT/DELETE gibi mutation endpoint'leri için
            // API anahtarının, tanımlı uygulamalardan herhangi birine ait olup olmadığını doğrula
            var apiKeysSection = configuration.GetSection("ApiKeys");
            var isValidKey = apiKeysSection.GetChildren()
                .Any(child => string.Equals(child.Value, extractedApiKey, StringComparison.Ordinal));

            if (!isValidKey)
            {
                context.Result = new UnauthorizedObjectResult(new { Message = "Invalid API Key. Access denied." });
                return;
            }
        }

        // Yetkilendirme başarılı
        await next();
    }
}
