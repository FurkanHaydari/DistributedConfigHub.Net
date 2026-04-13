using System.Net;
using System.Net.Http.Json;
using DistributedConfigHub.Application.DTOs;
using DistributedConfigHub.Domain.Entities;
using Xunit;

namespace DistributedConfigHub.IntegrationTests;

public class RollbackIntegrationTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Rollback_ShouldRevertValue_And_CreateRollbackAuditLogWithReason()
    {
        var client = factory.CreateAuthenticatedClient();

        // 1. Get configurations to find a seeded config
        var getResponse = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        getResponse.EnsureSuccessStatusCode();

        var configs = await getResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        Assert.NotNull(configs);
        
        var targetConfig = configs.First(c => c.Name == "ExternalPaymentApiUrl");
        string originalValue = targetConfig.Value;

        // 2. Put a new value (generates UPDATE audit log)
        var putPayload = new { id = targetConfig.Id, value = "Patates" };
        var putResponse = await client.PutAsJsonAsync($"/Configurations/{targetConfig.Id}", putPayload);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        // 3. Fetch History to find the UPDATE audit log
        var historyResponse = await client.GetAsync($"/Configurations/{targetConfig.Id}/history");
        historyResponse.EnsureSuccessStatusCode();
        var historyLogs = await historyResponse.Content.ReadFromJsonAsync<List<AuditLog>>(factory.DefaultJsonOptions);
        
        Assert.NotNull(historyLogs);
        // We expect at least one UPDATE log
        var updateLog = historyLogs.FirstOrDefault(l => l.Action == "UPDATE");
        Assert.NotNull(updateLog);

        // 4. Trigger Rollback to the previous state using the updateLog Id
        var rollbackResponse = await client.PostAsync($"/Configurations/{targetConfig.Id}/rollback/{updateLog.Id}", null);
        Assert.Equal(HttpStatusCode.OK, rollbackResponse.StatusCode);

        // 5. Verify the config value was reverted
        var revertedConfigResponse = await client.GetAsync($"/Configurations/{targetConfig.Id}");
        var revertedConfig = await revertedConfigResponse.Content.ReadFromJsonAsync<ConfigurationDto>(factory.DefaultJsonOptions);
        Assert.Equal(originalValue, revertedConfig!.Value);

        // 6. Verify that a new ROLLBACK audit log was generated
        var latestHistoryResponse = await client.GetAsync($"/Configurations/{targetConfig.Id}/history");
        var latestHistoryLogs = await latestHistoryResponse.Content.ReadFromJsonAsync<List<AuditLog>>(factory.DefaultJsonOptions);
        
        var rollbackLog = latestHistoryLogs!.FirstOrDefault(l => l.Action == "ROLLBACK");
        Assert.NotNull(rollbackLog);
        Assert.Contains(updateLog.Id.ToString(), rollbackLog.Reason);
    }
}
