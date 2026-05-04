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

        // Act: Send wrong key for SERVICE-A
        var response = await client.GetAsync("/Configurations?applicationName=SERVICE-A&environment=prod");

        // Assert: Our Attribute returns 401 Unauthorized when there's no match
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetConfigurations_FromOtherApplication_ShouldReturnForbidden()
    {
        var client = factory.CreateAuthenticatedClient();

        // Act: Try to access SERVICE-B details (Isolation Violation Attempt)
        var response = await client.GetAsync("/Configurations?applicationName=SERVICE-B&environment=prod");

        // Assert: We should get 403 Forbidden because the key does not belong to SERVICE-B
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
