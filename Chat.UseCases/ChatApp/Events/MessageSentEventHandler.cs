using Chat.Core.MessageAgg.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Chat.UseCases.ChatApp.Events;

public class MessageSentEventHandler(ILogger<MessageSentEventHandler> logger) : INotificationHandler<MessageSentEvent>
{
  private readonly ILogger<MessageSentEventHandler> _logger = logger;

  public ValueTask Handle(MessageSentEvent notification, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Message sent: {MessageId}", notification.Message.Id);
    return ValueTask.CompletedTask;
  }
}
