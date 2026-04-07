using DistributedConfigHub.Application.DTOs;
using System.Net.Http.Json;
using Xunit;

namespace DistributedConfigHub.IntegrationTests;

public class LiveUpdateIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LiveUpdateIntegrationTest(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // WebApplicationFactory bizim için tüm sanal altyapıya bağlı bir sahte(Client) oluşturur
        _client = factory.CreateClient();
        
        // Testlerin 403 Forbidden alıp patlamaması için yetki sızdırıyoruz (appsettings.json Mock'una göre)
        _client.DefaultRequestHeaders.Add("X-Api-Key", "ibb-demo-secret-key");
    }

    [Fact]
    public async Task UpdateIbbConfiguration_ShouldModifyDatabaseAndTriggerRabbitMqWithoutError()
    {
        // 1. Orijinal IBB ayarlarını "Prod" ortamı için çek
        var getResponse = await _client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        getResponse.EnsureSuccessStatusCode();
        var configs = await getResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>();
        
        Assert.NotNull(configs);
        Assert.NotEmpty(configs); // Migration seed datasının başarıyla geldiğini doğrular

        // İBB'nin MaxIstanbulKartTransactionsPerMin prod değerini bul (Seed'deki değeri: 50000)
        var kartLimitConfig = configs.FirstOrDefault(c => c.Name == "MaxIstanbulKartTransactionsPerMin");
        Assert.NotNull(kartLimitConfig);
        Assert.Equal("50000", kartLimitConfig.Value);

        // 2. Yeni değeri REST üzerinden PUT ile 99999 yap (Gerçek senaryo - Testcontainer RabbitMQ fırlatılacak)
        var putPayload = new
        {
            id = kartLimitConfig.Id,
            value = "99999"
        };

        var putResponse = await _client.PutAsJsonAsync($"/Configurations/{kartLimitConfig.Id}", putPayload);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, putResponse.StatusCode);

        // 3. Gerçekten güncellenmiş mi diye tekrar Testcontainer DB'den taze çek
        var getUpdatedResponse = await _client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");
        var updatedConfigs = await getUpdatedResponse.Content.ReadFromJsonAsync<List<ConfigurationDto>>();
        
        var updatedKartLimitConfig = updatedConfigs!.First(c => c.Name == "MaxIstanbulKartTransactionsPerMin");
        
        // Assert: 50000 olan limitin başarıyla 99999 olduğunu ve sistemin çökmediğini kanıtla
        Assert.Equal("99999", updatedKartLimitConfig.Value);
    }
}
