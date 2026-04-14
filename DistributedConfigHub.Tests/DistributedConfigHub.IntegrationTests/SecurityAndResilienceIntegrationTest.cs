using DistributedConfigHub.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace DistributedConfigHub.IntegrationTests;

public class SecurityAndResilienceIntegrationTest(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task GetConfigurations_WithInvalidApiKey_ShouldReturnUnauthorized()
    {
        var client = factory.CreateAuthenticatedClient("wrong-key-123");

        // Act: SERVICE-A için yanlış anahtar gönderiyoruz
        var response = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");

        // Assert: Attribute'umuz eşleşme olmayınca 401 Unauthorized döner
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConfigurations_FromOtherApplication_ShouldReturnForbidden()
    {
        var client = factory.CreateAuthenticatedClient();

        // Act: SERVICE-B bilgilerine erişmeye çalışıyoruz (İzolasyon İhlali Denemesi)
        var response = await client.GetAsync("/Configurations?applicationName=SERVICE-B&environment=prod");

        // Assert: 403 Forbidden almalıyız çünkü anahtar SERVICE-B'ye ait değil
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetConfigurations_WithCorrectKey_ShouldReturnData()
    {
        var client = factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");

        // Assert
        response.EnsureSuccessStatusCode();
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationDto>>(factory.DefaultJsonOptions);
        Assert.NotEmpty(configs!);
    }
}
