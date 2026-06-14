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
const int maxRetries = 10;

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        connection = await factory.CreateConnectionAsync();
        Console.WriteLine("Connected to RabbitMQ.");
        break;
    }
    catch
    {
        Console.WriteLine($"RabbitMQ down. Retry {attempt}/{maxRetries}...");
        await Task.Delay(3000);
    }
}

if (connection is null)
{
    Console.WriteLine("Could not connect to RabbitMQ.");
    return;
}

await using var connectionRef = connection;

// Declare topology once on a dedicated setup channel
await using (var setupChannel = await connection.CreateChannelAsync())
{
    await setupChannel.ExchangeDeclareAsync("dlx-exchange", ExchangeType.Direct, durable: true);
    await setupChannel.QueueDeclareAsync("dead-letter-queue", durable: true, exclusive: false, autoDelete: false);
    await setupChannel.QueueBindAsync("dead-letter-queue", "dlx-exchange", "dead-letter");

    var queueArgs = new Dictionary<string, object?>
    {
        { "x-dead-letter-exchange", "dlx-exchange" },
        { "x-dead-letter-routing-key", "dead-letter" }
    };

    await setupChannel.QueueDeclareAsync("main-queue", durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
}

const int consumerCount = 8;
const int prefetchPerConsumer = 500;
const int ackBatchSize = 200;
var ackFlushInterval = TimeSpan.FromMilliseconds(200);

long totalProcessed = 0;
long totalDeadLettered = 0;

var channels = new List<IChannel>();
var flushTasks = new List<Task>();
var cts = new CancellationTokenSource();

for (int i = 0; i < consumerCount; i++)
{
    var ch = await connection.CreateChannelAsync();
    channels.Add(ch);

    await ch.BasicQosAsync(prefetchSize: 0, prefetchCount: prefetchPerConsumer, global: false);

    ulong lastDeliveryTag = 0;
    int sinceLastAck = 0;
    var ackLock = new object();

    async Task FlushAckAsync()
    {
        ulong tagToAck;
        lock (ackLock)
        {
            if (sinceLastAck == 0) return;
            tagToAck = lastDeliveryTag;
            sinceLastAck = 0;
        }
        await ch.BasicAckAsync(tagToAck, multiple: true);
    }

    // Periodic flush so low-traffic moments don't leave acks pending too long
    var ackFlushTask = Task.Run(async () =>
    {
        while (!cts.IsCancellationRequested)
        {
            try { await Task.Delay(ackFlushInterval, cts.Token); }
            catch (TaskCanceledException) { break; }

            try { await FlushAckAsync(); } catch { }
        }
    });
    flushTasks.Add(ackFlushTask);

    var consumer = new AsyncEventingBasicConsumer(ch);
    consumer.ReceivedAsync += async (sender, ea) =>
    {
        var message = Encoding.UTF8.GetString(ea.Body.Span);

        if (message.Contains("9999"))
        {
            await ch.BasicRejectAsync(ea.DeliveryTag, requeue: false);
            var dl = Interlocked.Increment(ref totalDeadLettered);
            Console.WriteLine($"[Consumer] Dead-lettered #{dl}: {message}");
            return;
        }

        bool flushNow;
        lock (ackLock)
        {
            lastDeliveryTag = ea.DeliveryTag;
            sinceLastAck++;
            flushNow = sinceLastAck >= ackBatchSize;
        }

        if (flushNow)
            await FlushAckAsync();

        var count = Interlocked.Increment(ref totalProcessed);
        if (count % 50000 == 0)
            Console.WriteLine($"[Consumer] Processed {count} messages total.");
    };

    await ch.BasicConsumeAsync("main-queue", autoAck: false, consumer: consumer);
}

Console.WriteLine($"{consumerCount} consumers started. Press Enter to exit.");
Console.ReadLine();

cts.Cancel();
await Task.WhenAll(flushTasks);

// Close channels cleanly (any leftover unacked batch will be redelivered on next run)
foreach (var ch in channels)
{
    try { await ch.CloseAsync(); } catch { }
}