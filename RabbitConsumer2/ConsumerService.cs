using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitConsumer2;

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
            {
                // Fix the message before republishing (as per original code logic)
                var fixedMessage = message.Replace("9999", "0000");
                var fixedBody = Encoding.UTF8.GetBytes(fixedMessage);
                // The original code had some logic that didn't seem to actually republish anything,
                // just defined fixedBody.
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch
        {
            await _channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
        }
    }
}
