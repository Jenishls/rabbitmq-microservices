using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitConsumer;

public class ConsumerService(IChannel channel)
{
    private readonly IChannel _channel = channel;

    public async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var message = Encoding.UTF8.GetString(ea.Body.Span);

        if (message.Contains("9999"))
        {
            await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
            return;
        }
    }

    public async Task ProcessMessageWithAckAsync(BasicDeliverEventArgs ea, Func<ulong, Task>? onProcessed = null)
    {
        var message = Encoding.UTF8.GetString(ea.Body.Span);

        if (message.Contains("9999"))
        {
            await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
        else
        {
            if (onProcessed != null)
            {
                await onProcessed(ea.DeliveryTag);
            }
        }
    }
}
