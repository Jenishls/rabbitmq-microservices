using RabbitMQ.Client;
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
        await Task.Delay(3000); // ✅ async-friendly
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

var props = new BasicProperties
{
    DeliveryMode = DeliveryModes.Persistent
};

const int batchSize = 10000; // Define a manageable batch size
var allTasks = new List<Task>();
int totalMessages = 100000002;
for (int i = 1; i <= totalMessages; i++)
{
    string message = $"Message #{i}";
    var body = Encoding.UTF8.GetBytes(message);

    // Add the task to the list, but DO NOT await it yet.
    allTasks.Add(channel.BasicPublishAsync(
        exchange: "main-exchange",
        routingKey: "main-routing",
        mandatory: false,
        basicProperties: props,
        body: body
    ).AsTask()); // Convert ValueTask to Task for easier management

    // Check if the batch limit is reached OR it's the last message
    if (allTasks.Count >= batchSize || i == totalMessages)
    {
        Console.WriteLine($"[Producer] Sending Batch of {Math.Min(batchSize, allTasks.Count)} messages...");

        // Await all tasks currently in the list (this is where concurrency is limited)
        await Task.WhenAll(allTasks);

        // Clear the task list and proceed to the next batch
        allTasks.Clear();

        if (i == totalMessages) break; // Exit after processing the final batch
    }

    if (i % 100000 == 0 && i != 100000002)
        Console.WriteLine($"[Producer] Successfully processed up to {i} messages.");
}


Console.WriteLine("[Producer] Done.");