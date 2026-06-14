using RabbitMQ.Client;
using System.Text;

namespace RabbitProducer;

public static class ProducerDuplicate
{
    public static async Task RunAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "127.0.0.1",
            UserName = "guest",
            Password = "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            TopologyRecoveryEnabled = true,
        };

        IConnection? connection = null;
        IChannel? channel = null;

        int maxRetries = 10;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                connection = await factory.CreateConnectionAsync();
                channel = await connection.CreateChannelAsync();
                Console.WriteLine("Connected to RabbitMQ.");
                break;
            }
            catch
            {
                Console.WriteLine($"RabbitMQ down. Retry {attempt}/{maxRetries}...");
                await Task.Delay(3000);
            }
        }

        if (connection is null || channel is null)
        {
            Console.WriteLine("Could not connect to RabbitMQ.");
            return;
        }

        await using var _ = connection;
        await using var __ = channel;

        await channel.ExchangeDeclareAsync(
            exchange: "main-exchange",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false
        );

        await channel.ExchangeDeclareAsync(
            exchange: "dlx-exchange",
            type: ExchangeType.Direct,
            durable: true
        );

        await channel.QueueDeclareAsync(
            queue: "dead-letter-queue",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.QueueBindAsync(
            queue: "dead-letter-queue",
            exchange: "dlx-exchange",
            routingKey: "dead-letter"
        );

        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", "dlx-exchange" },
            { "x-dead-letter-routing-key", "dead-letter" }
        };

        try
        {
            await channel.QueueDeclareAsync(
                queue: "main-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs
            );
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException ex)
            when (ex.ShutdownReason?.ReplyCode == 406)
        {
            channel = await connection.CreateChannelAsync();
            await channel.QueueDeleteAsync("main-queue", ifUnused: false, ifEmpty: false);
            await channel.QueueDeclareAsync(
                queue: "main-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs
            );
            Console.WriteLine("[Producer] Recreated main-queue with correct DLX args.");
        }

        await channel.QueueBindAsync(
            queue: "main-queue",
            exchange: "main-exchange",
            routingKey: "main-routing"
        );

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        for (int i = 1; i <= 100000002; i++)
        {
            string message = $"Message #{i}";
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "main-exchange",
                routingKey: "main-routing",
                mandatory: false,
                basicProperties: props,
                body: body
            );

            if (i % 1000 == 0)
                Console.WriteLine($"[Producer] Sent {i} messages...");
        }

        Console.WriteLine("[Producer] Done.");
    }
}
