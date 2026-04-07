using DistributedConfigHub.Client;
using Microsoft.AspNetCore.Mvc;

namespace DemoConsumerApp.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController(IConfigSdkService configService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var siteName = configService.GetString("SiteName");
        var maxUsers = configService.GetInt("MaxUsers", 100);
        var isFeatureEnabled = configService.GetBoolean("FeatureX_Enabled");

        return Ok(new
        {
            SiteName = siteName ?? "Not Configured",
            MaxUsers = maxUsers,
            FeatureX_Enabled = isFeatureEnabled
        });
    }
}
