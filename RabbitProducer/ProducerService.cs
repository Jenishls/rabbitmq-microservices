using RabbitMQ.Client;
using System.Text;

namespace RabbitProducer;

public class ProducerService(IChannel channel)
{
    private readonly IChannel _channel = channel;

    public async Task PublishMessageAsync(string exchange, string routingKey, string message, BasicProperties? properties = null)
    {
        var body = Encoding.UTF8.GetBytes(message);
        await _channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties ?? new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
            body: body
        );
    }

    public async Task PublishBatchAsync(string exchange, string routingKey, IEnumerable<string> messages, int batchSize, BasicProperties? properties = null)
    {
        var props = properties ?? new BasicProperties { DeliveryMode = DeliveryModes.Persistent };
        var allTasks = new List<Task>();
        int count = 0;
        var messageList = messages.ToList();

        for (int i = 0; i < messageList.Count; i++)
        {
            var body = Encoding.UTF8.GetBytes(messageList[i]);
            allTasks.Add(_channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body
            ).AsTask());

            if (allTasks.Count >= batchSize || i == messageList.Count - 1)
            {
                await Task.WhenAll(allTasks);
                count += allTasks.Count;
                allTasks.Clear();
            }
        }
    }
}
