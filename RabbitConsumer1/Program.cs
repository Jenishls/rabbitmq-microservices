using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

Console.WriteLine("Starting...");

var factory = new ConnectionFactory
{
    Uri = new Uri("amqps://ckhqfotb:TUgSZsFKpukPUFafxylUGuVe0Et5HiA8@kebnekaise.lmq.cloudamqp.com/ckhqfotb"),
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

// 1. Declare the DLX and DLQ first
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

// 2. Declare main-queue with DLX arguments
var queueArgs = new Dictionary<string, object?>
{
    { "x-dead-letter-exchange", "dlx-exchange" },
    { "x-dead-letter-routing-key", "dead-letter" }
};

await channel.QueueDeclareAsync(
    queue: "main-queue",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: queueArgs         // ← binds DLX to this queue
);

var consumer = new AsyncEventingBasicConsumer(channel);

consumer.ReceivedAsync += async (sender, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"[Consumer] Received: {message}");

    try
    {
        if (message.Contains("9999"))
            throw new Exception("Bad message");

        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        if (ea.DeliveryTag % 10000 == 0)
        Console.WriteLine($"[Consumer] Acked: {message}");
    }
    catch
    {
        Console.WriteLine($"[Consumer] Moving to DLQ: {message}");
        await channel.BasicRejectAsync(ea.DeliveryTag, requeue: false);
    }
};

// 3. Set prefetch so messages aren't buffered invisibly
await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10000, global: false);

await channel.BasicConsumeAsync(
    queue: "main-queue",
    autoAck: false,
    consumer: consumer
);

Console.WriteLine("Consumer started. Press Enter to exit.");
Console.ReadLine();