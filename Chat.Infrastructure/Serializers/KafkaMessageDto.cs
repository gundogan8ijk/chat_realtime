using System;
using ProtoBuf;

namespace Chat.Infrastructure.Serializers;

[ProtoContract]
public class KafkaMessageDto
{
  [ProtoMember(1)] public Guid Id { get; set; }
  [ProtoMember(2)] public Guid SenderUserId { get; set; }
  [ProtoMember(3)] public Guid ReceiverId { get; set; }
  [ProtoMember(4)] public string? MessageBody { get; set; }
  [ProtoMember(5)] public DateTime CreateDate { get; set; }
  [ProtoMember(6)] public string MessageType { get; set; } = "Text";
  [ProtoMember(7)] public Guid? ParentMessageId { get; set; }
}
