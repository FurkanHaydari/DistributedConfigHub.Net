using DistributedConfigHub.Application.Features.Commands;
using DistributedConfigHub.Application.Interfaces;
using DistributedConfigHub.Domain.Entities;
using DistributedConfigHub.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace DistributedConfigHub.UnitTests.Application;

public class UpdateConfigurationCommandHandlerTests
{
    private readonly Mock<IConfigurationRepository> _repositoryMock;
    private readonly Mock<IMessagePublisher> _messagePublisherMock;
    private readonly UpdateConfigurationCommandHandler _handler;

    public UpdateConfigurationCommandHandlerTests()
    {
        _repositoryMock = new Mock<IConfigurationRepository>();
        _messagePublisherMock = new Mock<IMessagePublisher>();
        _handler = new UpdateConfigurationCommandHandler(_repositoryMock.Object, _messagePublisherMock.Object);
    }

    [Fact]
    public async Task Handle_WhenRecordDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var command = new UpdateConfigurationCommand(Guid.NewGuid(), "NewValue", "TEST-APP");
        _repositoryMock.Setup(repo => repo.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConfigurationRecord?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
        _repositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<ConfigurationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _messagePublisherMock.Verify(pub => pub.PublishConfigurationUpdatedEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

[Fact]
public async Task Handle_WhenRecordExists_ShouldUpdateAndPublishEventAndReturnTrue()
{
    // Arrange
    var record = new ConfigurationRecord(
        "TestName", 
        ConfigurationType.String, 
        "OldValue", 
        "TEST-APP", 
        "prod");


    var command = new UpdateConfigurationCommand(record.Id, "SuccessValue", "TEST-APP");

    _repositoryMock.Setup(repo => repo.GetByIdAsync(record.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(record);

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.Should().BeTrue();
    
    // Değer güncellenmiş mi?
    record.Value.Should().Be("SuccessValue");
    
    // Kuralımız çalışmış mı?
    record.UpdatedBy.Should().Be("admin"); 
    
    // Veritabanı ve RabbitMQ çağrılmış mı?
    _repositoryMock.Verify(repo => repo.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    _messagePublisherMock.Verify(pub => pub.PublishConfigurationUpdatedEventAsync("TEST-APP", "prod", It.IsAny<CancellationToken>()), Times.Once);
}
}
