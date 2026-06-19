namespace Chat.Core.MessageAgg.Events;

public sealed class MessageSentEvent(Message message) : DomainEventBase
{
  public Message Message { get; init; } = message;
}

