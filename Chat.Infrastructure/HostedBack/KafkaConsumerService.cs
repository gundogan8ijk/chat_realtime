using Confluent.Kafka;
using System.Threading.Channels;



using Chat.Infrastructure.Serializers;
using Microsoft.Extensions.Hosting;

namespace Chat.Infrastructure.HostedBack;

public class KafkaConsumerService : BackgroundService
{
  private readonly IConfiguration _config;
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<KafkaConsumerService> _logger;
  private readonly IConsumer<string, byte[]> _consumer;
  private readonly Channel<ConsumeResult<string, byte[]>> _channel;

  private const int BatchSize = 100;
  private static readonly TimeSpan BatchDelay = TimeSpan.FromMilliseconds(500);

  public KafkaConsumerService(
      IConfiguration config,
      IServiceScopeFactory scopeFactory,
      ILogger<KafkaConsumerService> logger)
  {
    _config = config;
    _scopeFactory = scopeFactory;
    _logger = logger;

    var consumerConfig = new ConsumerConfig
    {
      BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
      GroupId = _config["Kafka:GroupId"] ?? "chat-consumer-group",
      AutoOffsetReset = AutoOffsetReset.Latest,
      EnableAutoCommit = false,
      EnableAutoOffsetStore = false,
    };

    _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
    var topic = _config["Kafka:Topic1"] ?? "chat-messages";
    _consumer.Subscribe(topic);

    _channel = Channel.CreateBounded<ConsumeResult<string, byte[]>>(
        new BoundedChannelOptions(2000)
        {
          SingleReader = true,
          SingleWriter = true,
          FullMode = BoundedChannelFullMode.Wait
        });
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation("=== KafkaConsumerService STARTING ===");

    var workerTask = WorkerLoop(stoppingToken);

    try
    {
      while (!stoppingToken.IsCancellationRequested)
      {
        try
        {
          var consumeResult = _consumer.Consume(stoppingToken);
          if (consumeResult != null)
          {
            await _channel.Writer.WriteAsync(consumeResult, stoppingToken);
          }
        }
        catch (OperationCanceledException)
        {
          break;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Kafka consume loop error");
        }
      }
    }
    finally
    {
      _channel.Writer.Complete();
      await workerTask;
    }
  }

  private async Task WorkerLoop(CancellationToken token)
  {
    var batchDb = new List<ConsumeResult<string, byte[]>>();
    var lastFlushTime = DateTime.UtcNow;

    try
    {
      await foreach (var msg in _channel.Reader.ReadAllAsync(token))
      {
        batchDb.Add(msg);

        bool collect = batchDb.Count >= BatchSize || 
                       (batchDb.Count > 0 && DateTime.UtcNow - lastFlushTime > BatchDelay);

        if (collect)
        {
          await ProcessBatchAsync(batchDb, token);
          batchDb.Clear();
          lastFlushTime = DateTime.UtcNow;
        }
      }
    }
    catch (OperationCanceledException)
    {
      // normal shutdown
    }

    if (batchDb.Any())
    {
      await ProcessBatchAsync(batchDb, token);
    }
  }

  private async Task ProcessBatchAsync(List<ConsumeResult<string, byte[]>> batch, CancellationToken ct)
  {
    var lastConsume = batch.Last();
    var messagesWithTs = batch.Select(x => new
    {
      Mess = ProtobufSerializer.Deserialize<Message>(x.Message.Value),
      KeyConChat = Guid.Parse(x.Message.Key),
      KeyUserCon = x.Message.Key + ProtobufSerializer.Deserialize<Message>(x.Message.Value).SenderUserId.Value.ToString(),
      Timespan = x.Message.Timestamp.UnixTimestampMs
    }).ToList();

    var listMess = messagesWithTs.Select(x => x.Mess).ToList();

    var latestConversation = messagesWithTs.GroupBy(x => x.KeyConChat)
                            .Select(g => g.MaxBy(x => x.Timespan))
                            .ToList();

    var listConver = latestConversation.Select(x => 
        new ConversationUpdateDto(
            ConversationId.From(x!.KeyConChat), 
            x.Mess.Id)
    ).ToList();

    var latestUserConversation = messagesWithTs.GroupBy(x => x.KeyUserCon)
                                .Select(g => g.MaxBy(x => x.Timespan))
                                .ToList();

    var listUserCon = latestUserConversation.Select(y => 
        new UserConversationUpdateDto(
            y!.Mess.SenderUserId, 
            ConversationId.From(y.KeyConChat), 
            y.Mess.Id)
    ).ToList();

    await TrySaveBatchAsync(listMess, listConver, listUserCon, lastConsume, ct);
  }

  private async Task TrySaveBatchAsync(
      List<Message> listMess, 
      List<ConversationUpdateDto> listConver,
      List<UserConversationUpdateDto> listUserCon, 
      ConsumeResult<string, byte[]> lastConsume,
      CancellationToken ct)
  {
    int retry = 0;
    const int maxRetry = 3;

    while (true)
    {
      try
      {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMessageRepository>();

        await repo.SaveBatchAsync(listMess, listConver, listUserCon, ct);

        if (lastConsume != null)
        {
          _consumer.Commit(lastConsume);
        }

        break;
      }
      catch (Exception ex)
      {
        retry++;
        _logger.LogError(ex, "Lỗi lưu database ở consumer. Thử lại {Retry}/{MaxRetry}", retry, maxRetry);

        if (retry >= maxRetry)
        {
          _logger.LogError("Quá số lần thử lại tối đa. Consumer sẽ khởi động lại.");
          throw;
        }

        await Task.Delay(1300, ct);
      }
    }
  }

  public override void Dispose()
  {
    _consumer?.Close();
    _consumer?.Dispose();
    base.Dispose();
  }
}

