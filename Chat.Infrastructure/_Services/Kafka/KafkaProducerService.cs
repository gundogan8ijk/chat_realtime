using Confluent.Kafka;

using Chat.Infrastructure.Serializers;

namespace Chat.Infrastructure._Services.Kafka;

public class KafkaProducerService : IDisposable
{
  private readonly ILogger<KafkaProducerService> _logger;
  private readonly IConfiguration _configuration;
  private readonly IProducer<string, byte[]> _producer;

  public KafkaProducerService(
      ILogger<KafkaProducerService> logger,
      IConfiguration configuration)
  {
    _logger = logger;
    _configuration = configuration;

    var config = new ProducerConfig
    {
      BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
      ClientId = $"producerSer-{Guid.NewGuid()}",
      Acks = Acks.Leader,
      MessageTimeoutMs = _configuration["Kafka:MessageTimeoutMs"] != null ? int.Parse(_configuration["Kafka:MessageTimeoutMs"]!) : 5000,
      BatchNumMessages = _configuration["Kafka:BatchNumMessages"] != null ? int.Parse(_configuration["Kafka:BatchNumMessages"]!) : 100,
      BatchSize = _configuration["Kafka:BatchSize"] != null ? int.Parse(_configuration["Kafka:BatchSize"]!) : 1000000,
      LingerMs = _configuration["Kafka:LingerMs"] != null ? int.Parse(_configuration["Kafka:LingerMs"]!) : 10,
      CompressionType = CompressionType.Lz4,
      AllowAutoCreateTopics = true,
      MaxInFlight = _configuration["Kafka:MaxInFlight"] != null ? int.Parse(_configuration["Kafka:MaxInFlight"]!) : 1,
      MessageSendMaxRetries = _configuration["Kafka:MessageSendMaxRetries"] != null ? int.Parse(_configuration["Kafka:MessageSendMaxRetries"]!) : 2,
    };

    _producer = new ProducerBuilder<string, byte[]>(config).Build();
    _logger.LogInformation("Kafka Producer khởi tạo thành công");
  }

  public Message<string, byte[]> CreateMsg<T>(T value, string? key)
  {
    return new Message<string, byte[]>()
    {
      Key = key ?? string.Empty,
      Value = ProtobufSerializer.Serialize(value),
      Timestamp = new Timestamp(DateTime.UtcNow)
    };
  }

  public async Task AddProduceAsync(string topic, Message<string, byte[]> message, CancellationToken cancellationToken)
  {
    try
    {
      var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
      _logger.LogInformation("Đã gửi message vào Kafka topic {Topic} offset:{Offset}", topic, deliveryResult.Offset);
    }
    catch (ProduceException<string, byte[]> e)
    {
      _logger.LogError("Gửi message thất bại: {Reason}", e.Error.Reason);
    }
  }

  public void Dispose()
  {
    _producer.Flush(TimeSpan.FromSeconds(10));
    _producer.Dispose();
  }
}

