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
        
        // 1. Orijinal ayarları "Prod" ortamı için çek
        var getResponse = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        getResponse.EnsureSuccessStatusCode();

        var configs = await getResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        
        Assert.NotNull(configs);
        Assert.NotEmpty(configs); // Migration seed datasının başarıyla geldiğini doğrular

        // MaxConcurrentTransactions prod değerini bul (Seed'deki değeri: 50000)
        var limitConfig = configs.FirstOrDefault(c => c.Name == "MaxConcurrentTransactions");
        Assert.NotNull(limitConfig);
        Assert.Equal("50000", limitConfig.Value);

        // 2. Yeni değeri REST üzerinden PUT ile 99999 yap (Gerçek senaryo - Testcontainer RabbitMQ fırlatılacak)
        var putPayload = new
        {
            id = limitConfig.Id,
            value = "99999"
        };

        var putResponse = await client.PutAsJsonAsync($"/Configurations/{limitConfig.Id}", putPayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, putResponse.StatusCode);

        // 3. Gerçekten güncellenmiş mi diye tekrar Testcontainer DB'den taze çek
        var getUpdatedResponse = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        var updatedConfigs = await getUpdatedResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        
        var updatedLimitConfig = updatedConfigs!.First(c => c.Name == "MaxConcurrentTransactions");
        
        // Assert: 50000 olan limitin başarıyla 99999 olduğunu ve sistemin çökmediğini kanıtla
        Assert.Equal("99999", updatedLimitConfig.Value);
    }
}
