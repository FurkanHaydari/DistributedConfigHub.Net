using DistributedConfigHub.Application.DTOs;
using System.Net.Http.Json;
using Xunit;

namespace DistributedConfigHub.IntegrationTests;

public class LiveUpdateIntegrationTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task UpdateConfiguration_ShouldModifyDatabaseAndTriggerRabbitMqWithoutError()
    {
        var client = factory.CreateAuthenticatedClient();
        
        // 1. Fetch original settings for the "Prod" environment
        var getResponse = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        getResponse.EnsureSuccessStatusCode();

        var configs = await getResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        
        Assert.NotNull(configs);
        Assert.NotEmpty(configs); // Verifies that Migration seed data arrived successfully

        // Find MaxConcurrentTransactions prod value (Value in Seed: 50000)
        var limitConfig = configs.FirstOrDefault(c => c.Name == "MaxConcurrentTransactions");
        Assert.NotNull(limitConfig);
        Assert.Equal("50000", limitConfig.Value);

        // 2. Update value to 99999 via REST PUT (Real scenario - Testcontainer RabbitMQ will be fired)
        var putPayload = new
        {
            id = limitConfig.Id,
            value = "99999"
        };

        var putResponse = await client.PutAsJsonAsync($"/Configurations/{limitConfig.Id}", putPayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, putResponse.StatusCode);

        // 3. Fetch fresh data from Testcontainer DB to verify it was actually updated
        var getUpdatedResponse = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        var updatedConfigs = await getUpdatedResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        
        var updatedLimitConfig = updatedConfigs!.First(c => c.Name == "MaxConcurrentTransactions");
        
        // Assert: Prove that the 50000 limit successfully became 99999 and the system did not crash
        Assert.Equal("99999", updatedLimitConfig.Value);
    }
}
