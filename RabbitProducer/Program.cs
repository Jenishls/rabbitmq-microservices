using RabbitMQ.Client;
using RabbitProducer;
using System.Text;

var factory = new ConnectionFactory
{
    Uri = new Uri("amqps://cotb:"),
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

// ✅ Declare exchange
await channel.ExchangeDeclareAsync(
    exchange: "main-exchange",
    type: ExchangeType.Direct,
    durable: true,
    autoDelete: false
);

// ✅ Declare DLX + DLQ (must match consumer)
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

// ✅ Declare main-queue with DLX args (must match consumer exactly)
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
// ✅ Bind main-queue to main-exchange
await channel.QueueBindAsync(
    queue: "main-queue",
    exchange: "main-exchange",
    routingKey: "main-routing"
);

var producerService = new ProducerService(channel);
var props = new BasicProperties
{
    DeliveryMode = DeliveryModes.Persistent
};

const int batchSize = 10;
int totalMessages = 100;
var messages = Enumerable.Range(1, totalMessages).Select(i => $"Message #{i}");

Console.WriteLine($"[Producer] Sending {totalMessages} messages in batches of {batchSize}...");
await producerService.PublishBatchAsync("main-exchange", "main-routing", messages, batchSize, props);

Console.WriteLine("[Producer] Done.");
