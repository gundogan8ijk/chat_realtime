using Chat.Infrastructure._Services.Kafka;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chat.Web.ChatApi;

public class KafkaTest(KafkaProducerService producer)
  : EndpointWithoutRequest<Ok<string>>
{
  public override void Configure()
  {
    Get("/kafka/test/testtttt");
    AllowAnonymous();
  }

  public override async Task<Ok<string>> ExecuteAsync(CancellationToken ct)
  {
    _ = producer;
    return TypedResults.Ok("Sent to Kafka → ");
  }
}

