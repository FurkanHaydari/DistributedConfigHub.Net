using System.Collections.Generic;
using System.Threading.Tasks;
using DistributedConfigHub.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace DistributedConfigHub.UnitTests.Api;

public class ApiKeyAuthorizeAttributeTests
{
    private readonly ApiKeyAuthorizeAttribute _filter;
    private readonly Mock<IConfiguration> _configurationMock;

    public ApiKeyAuthorizeAttributeTests()
    {
        _filter = new ApiKeyAuthorizeAttribute();
        _configurationMock = new Mock<IConfiguration>();
    }

    private ActionExecutingContext CreateMockContext(string? apiKeyHeader, string? applicationNameQuery)
    {
        var httpContext = new DefaultHttpContext();
        
        // 1. Setup Header
        if (apiKeyHeader is not null)
        {
            httpContext.Request.Headers["X-Api-Key"] = apiKeyHeader;
        }

        // 2. Setup Query String
        if (applicationNameQuery is not null)
        {
            httpContext.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "applicationName", applicationNameQuery }
            });
        }

        // 3. Setup Dependency Injection for IConfiguration
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(_configurationMock.Object);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        // 4. Build Filter Context
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor()
        );

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object()
        );
    }

    private void SetupApiKeys(Dictionary<string, string> keys)
    {
        var sectionMock = new Mock<IConfigurationSection>();
        var children = keys.Select(kvp =>
        {
            var child = new Mock<IConfigurationSection>();
            child.Setup(x => x.Key).Returns(kvp.Key);
            child.Setup(x => x.Value).Returns(kvp.Value);
            return child.Object;
        }).ToList();

        sectionMock.Setup(x => x.GetChildren()).Returns(children);
        _configurationMock.Setup(x => x.GetSection("ApiKeys")).Returns(sectionMock.Object);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApiKeyMissing_ShouldReturn401Unauthorized()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: null, applicationNameQuery: "SERVICE-A");
        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenInvalidKey_ShouldReturn401Unauthorized()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "invalid-key", applicationNameQuery: null);
        SetupApiKeys(new Dictionary<string, string> { { "SERVICE-A", "real-key" } });

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenValidKey_ShouldProceed()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "real-secret-key", applicationNameQuery: null);
        SetupApiKeys(new Dictionary<string, string> { { "SERVICE-A", "real-secret-key" } });
        
        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeNull(); 
        context.HttpContext.Items["CallerApplicationName"].Should().Be("SERVICE-A");
        nextMock.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenNoConfigurationMatches_ShouldReturn401Unauthorized()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "valid-key", applicationNameQuery: "UNKNOWN-APP");
        SetupApiKeys(new Dictionary<string, string> { { "REAL-APP", "different-key" } });

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApiKeyIsWrong_ShouldReturn401Unauthorized()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "hacker-key", applicationNameQuery: "SERVICE-A");
        SetupApiKeys(new Dictionary<string, string> { { "SERVICE-A", "real-secret-key" } });

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>(); 
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApiKeyMatches_ShouldProceedAndSetCallerName()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "real-secret-key", applicationNameQuery: "SERVICE-A");
        SetupApiKeys(new Dictionary<string, string> { { "SERVICE-A", "real-secret-key" } });

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeNull();
        context.HttpContext.Items["CallerApplicationName"].Should().Be("SERVICE-A");
        nextMock.Verify(x => x(), Times.Once);
    }
}