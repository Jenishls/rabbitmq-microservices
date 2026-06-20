using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitConsumer1;

public class ConsumerService(IChannel channel)
{
    private readonly IChannel _channel = channel;

    public async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            if (message.Contains("9999"))
                throw new Exception("Bad message");

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch
        {
            await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
    }
}
