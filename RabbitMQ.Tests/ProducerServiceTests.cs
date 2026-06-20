using Moq;
using RabbitMQ.Client;
using RabbitProducer;
using System.Text;

namespace RabbitMQ.Tests;

public class ProducerServiceTests
{
    private readonly Mock<IChannel> _mockChannel;
    private readonly ProducerService _producerService;

    public ProducerServiceTests()
    {
        _mockChannel = new Mock<IChannel>();
        _producerService = new ProducerService(_mockChannel.Object);
    }

    [Fact]
    public async Task PublishMessageAsync_CallsBasicPublishAsyncWithCorrectParameters()
    {
        // Arrange
        var exchange = "test-exchange";
        var routingKey = "test-routing";
        var message = "test-message";
        var body = Encoding.UTF8.GetBytes(message);

        // Act
        await _producerService.PublishMessageAsync(exchange, routingKey, message);

        // Assert
        _mockChannel.Verify(c => c.BasicPublishAsync(
            It.Is<string>(e => e == exchange),
            It.Is<string>(r => r == routingKey),
            It.Is<bool>(m => m == false),
            It.IsAny<BasicProperties>(),
            It.Is<ReadOnlyMemory<byte>>(b => b.ToArray().SequenceEqual(body)),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Fact]
    public async Task PublishBatchAsync_CallsBasicPublishAsyncForAllMessages()
    {
        // Arrange
        var exchange = "test-exchange";
        var routingKey = "test-routing";
        var messages = new List<string> { "msg1", "msg2", "msg3" };
        var batchSize = 2;

        // Act
        await _producerService.PublishBatchAsync(exchange, routingKey, messages, batchSize);

        // Assert
        _mockChannel.Verify(c => c.BasicPublishAsync(
            It.Is<string>(e => e == exchange),
            It.Is<string>(r => r == routingKey),
            It.IsAny<bool>(),
            It.IsAny<BasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>(),
            It.IsAny<CancellationToken>()
        ), Times.Exactly(messages.Count));
    }
}
