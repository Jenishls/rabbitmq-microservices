using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMQ.Tests;

public class ConsumerServiceTests
{
    [Fact]
    public async Task RabbitConsumer_ProcessMessageWithAckAsync_RejectsBadMessage()
    {
        // Arrange
        var mockChannel = new Mock<IChannel>();
        var service = new RabbitConsumer.ConsumerService(mockChannel.Object);
        var message = "Error 9999 occurred";
        var body = Encoding.UTF8.GetBytes(message);
        var ea = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: default
        );

        // Act
        await service.ProcessMessageWithAckAsync(ea);

        // Assert
        mockChannel.Verify(c => c.BasicRejectAsync(123, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RabbitConsumer_ProcessMessageWithAckAsync_InvokesOnProcessedForGoodMessage()
    {
        // Arrange
        var mockChannel = new Mock<IChannel>();
        var service = new RabbitConsumer.ConsumerService(mockChannel.Object);
        var message = "Good message";
        var body = Encoding.UTF8.GetBytes(message);
        var ea = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 123,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: default
        );
        bool processedCalled = false;

        // Act
        await service.ProcessMessageWithAckAsync(ea, (tag) => { processedCalled = true; return Task.CompletedTask; });

        // Assert
        Assert.True(processedCalled);
        mockChannel.Verify(c => c.BasicRejectAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RabbitConsumer1_HandleMessageAsync_AcksGoodMessage()
    {
        // Arrange
        var mockChannel = new Mock<IChannel>();
        var service = new RabbitConsumer1.ConsumerService(mockChannel.Object);
        var message = "Good message";
        var body = Encoding.UTF8.GetBytes(message);
        var ea = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 456,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: default
        );

        // Act
        await service.HandleMessageAsync(ea);

        // Assert
        mockChannel.Verify(c => c.BasicAckAsync(456, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RabbitConsumer1_HandleMessageAsync_RejectsBadMessage()
    {
        // Arrange
        var mockChannel = new Mock<IChannel>();
        var service = new RabbitConsumer1.ConsumerService(mockChannel.Object);
        var message = "Bad 9999 message";
        var body = Encoding.UTF8.GetBytes(message);
        var ea = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 456,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: default
        );

        // Act
        await service.HandleMessageAsync(ea);

        // Assert
        mockChannel.Verify(c => c.BasicRejectAsync(456, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RabbitConsumer2_HandleMessageAsync_AcksMessage()
    {
        // Arrange
        var mockChannel = new Mock<IChannel>();
        var service = new RabbitConsumer2.ConsumerService(mockChannel.Object);
        var message = "Any message";
        var body = Encoding.UTF8.GetBytes(message);
        var ea = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 789,
            redelivered: false,
            exchange: "exchange",
            routingKey: "routing",
            properties: new BasicProperties(),
            body: body,
            cancellationToken: default
        );

        // Act
        await service.HandleMessageAsync(ea);

        // Assert
        mockChannel.Verify(c => c.BasicAckAsync(789, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
