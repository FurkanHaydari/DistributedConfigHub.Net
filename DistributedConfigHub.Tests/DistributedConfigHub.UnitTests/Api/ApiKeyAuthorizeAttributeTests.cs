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
        nextMock.Verify(x => x(), Times.Never); // Handler'a inememeli
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApplicationNameMissing_ShouldReturn400BadRequest()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "some-key", applicationNameQuery: null);
        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<BadRequestObjectResult>();
        nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenNoConfigurationMatches_ShouldReturn401Unauthorized()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "valid-key", applicationNameQuery: "UNKNOWN-APP");
        
        // Mock appsettings.json to return null for UNKNOWN-APP
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(x => x.Value).Returns((string?)null);
        _configurationMock.Setup(x => x.GetSection("ApiKeys:UNKNOWN-APP")).Returns(sectionMock.Object);

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApiKeyDoesNotMatch_ShouldReturn403Forbidden()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "hacker-key", applicationNameQuery: "SERVICE-A");
        
        // Mock appsettings.json to return real key for SERVICE-A
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(x => x.Value).Returns("real-secret-key");
        _configurationMock.Setup(x => x.GetSection("ApiKeys:SERVICE-A")).Returns(sectionMock.Object);

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        var result = context.Result.Should().BeOfType<StatusCodeResult>().Subject;
        result.StatusCode.Should().Be(403); // Forbidden
        nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenApiKeyMatches_ShouldProceedToNextDelegate()
    {
        // Arrange
        var context = CreateMockContext(apiKeyHeader: "real-secret-key", applicationNameQuery: "SERVICE-A");
        
        // Mock appsettings.json
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(x => x.Value).Returns("real-secret-key");
        _configurationMock.Setup(x => x.GetSection("ApiKeys:SERVICE-A")).Returns(sectionMock.Object);

        var nextMock = new Mock<ActionExecutionDelegate>();

        // Act
        await _filter.OnActionExecutionAsync(context, nextMock.Object);

        // Assert
        context.Result.Should().BeNull(); // Null means it didn't block
        nextMock.Verify(x => x(), Times.Once); // Handler çalışmalı!
    }
}
