using System.Net;
using System.Text.Json;
using DistributedConfigHub.Client.Models;
using DistributedConfigHub.Client.Services;
using DistributedConfigHub.Client.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace DistributedConfigHub.UnitTests.Client;

public class ConfigSdkServiceTests
{
    private readonly DistributedConfigOptions _options;
    private readonly Mock<ILogger<ConfigSdkService>> _loggerMock;
    private readonly string _fallbackFilePath;

    public ConfigSdkServiceTests()
    {
        _fallbackFilePath = Path.Combine(Path.GetTempPath(), $"test-fallback-{Guid.NewGuid()}.json");
        _options = new DistributedConfigOptions
        {
            ApiBaseUrl = "http://fake-api",
            ApplicationName = "TEST-APP",
            Environment = "dev",
            ApiKey = "test-secret-key",
            FallbackFilePath = _fallbackFilePath
        };
        _loggerMock = new Mock<ILogger<ConfigSdkService>>();
    }

    [Fact]
    public async Task ReloadConfigurationsAsync_WhenApiIsSuccessful_ShouldCacheDataAndWriteToFallbackFile()
    {
        // Arrange
        var apiResponse = new List<ConfigurationItem>
        {
            new("SiteName", "String", "TestSite", "TEST-APP", "dev", true)
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(apiResponse))
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_options.ApiBaseUrl) };
        var sdkService = new ConfigSdkService(httpClient, _options, _loggerMock.Object);

        // Act
        await sdkService.ReloadConfigurationsAsync();

        // Assert
        sdkService.GetString("SiteName").Should().Be("TestSite");
        File.Exists(_fallbackFilePath).Should().BeTrue();

        var writtenContent = await File.ReadAllTextAsync(_fallbackFilePath);
        writtenContent.Should().Contain("TestSite");

        // Clean up
        if (File.Exists(_fallbackFilePath)) File.Delete(_fallbackFilePath);
    }

    [Fact]
    public async Task ReloadConfigurationsAsync_WhenApiFails_ShouldLoadFromFallbackFile()
    {
        // Arrange
        var fallbackData = new List<ConfigurationItem>
        {
            new("MaxLimits", "Int", "999", "TEST-APP", "dev", true)
        };
        await File.WriteAllTextAsync(_fallbackFilePath, JsonSerializer.Serialize(fallbackData));

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_options.ApiBaseUrl) };
        var sdkService = new ConfigSdkService(httpClient, _options, _loggerMock.Object);

        // Act
        await sdkService.ReloadConfigurationsAsync(); // API fail olacak, file okuyacak

        // Assert
        sdkService.GetInt("MaxLimits").Should().Be(999);

        // Clean up
        if (File.Exists(_fallbackFilePath)) File.Delete(_fallbackFilePath);
    }
    [Fact]
    public async Task ReloadConfigurationsAsync_WhenApiReturnsEmptyAndCacheIsNotEmpty_ShouldKeepExistingCache()
    {
        // Arrange
        var initialData = new List<ConfigurationItem> { new("App:Theme", "String", "Dark", "TEST-APP", "dev", true) };
        var emptyData = new List<ConfigurationItem>();

        var handlerMock = new Mock<HttpMessageHandler>();
        var responseCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => {
                var content = responseCount == 0 ? initialData : emptyData;
                responseCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(content))
                };
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_options.ApiBaseUrl) };
        var sdkService = new ConfigSdkService(httpClient, _options, _loggerMock.Object);

        // Act & Assert
        await sdkService.ReloadConfigurationsAsync(); // First load: Success
        sdkService.GetString("App:Theme").Should().Be("Dark");

        await sdkService.ReloadConfigurationsAsync(); // Second load: Empty
        sdkService.GetString("App:Theme").Should().Be("Dark"); // Should NOT be null
    }

    [Fact]
    public async Task ReloadConfigurationsAsync_WhenApiFailsAndCacheIsNotEmpty_ShouldMaintainCurrentCache()
    {
        // Arrange
        var initialData = new List<ConfigurationItem> { new("App:Lang", "String", "TR", "TEST-APP", "dev", true) };

        var handlerMock = new Mock<HttpMessageHandler>();
        var responseCount = 0;
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => {
                if (responseCount == 0)
                {
                    responseCount++;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(JsonSerializer.Serialize(initialData))
                    };
                }
                return new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError };
            });

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_options.ApiBaseUrl) };
        var sdkService = new ConfigSdkService(httpClient, _options, _loggerMock.Object);

        // Act
        await sdkService.ReloadConfigurationsAsync(); // Initial load
        await sdkService.ReloadConfigurationsAsync(); // Fails load

        // Assert
        sdkService.GetString("App:Lang").Should().Be("TR"); // Memory cache should be preserved
    }
}
