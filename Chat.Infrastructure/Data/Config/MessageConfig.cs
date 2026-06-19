


using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chat.Infrastructure.Data.Config;

public class MessageConfig : IEntityTypeConfiguration<Message>
{
  public void Configure(EntityTypeBuilder<Message> builder)
  {
    builder.ToTable("Messages");

    builder.HasKey(m => m.Id);
    builder.Property(m => m.Id)
        .HasConversion(new ValueConverter<MessageId, Guid>(id => id.Value, v => MessageId.From(v)));

    builder.Property(m => m.SenderUserId)
        .HasConversion(new ValueConverter<UserId, Guid>(id => id.Value, v => UserId.From(v)))
        .IsRequired();

    builder.Property(m => m.ReceiverId)
        .HasConversion(new ValueConverter<UserId, Guid>(id => id.Value, v => UserId.From(v)))
        .IsRequired();

    builder.Property(m => m.MessageBody)
        .HasConversion(new ValueConverter<MessageContent, string>(b => b.Value, v => MessageContent.From(v)))
        .HasMaxLength(1000)
        .IsRequired(false);

    builder.Property(m => m.CreateDate)
        .IsRequired();

    builder.Property(m => m.MessageType)
        .HasConversion(new ValueConverter<MessageType, int>(t => t.Value, v => MessageType.FromValue(v)))
        .IsRequired();

    builder.Property(m => m.Status)
        .HasConversion(new ValueConverter<DeliveryStatus, int>(s => s.Value, v => DeliveryStatus.FromValue(v)))
        .IsRequired();

    builder.Property(m => m.ParentMessageId)
        .HasConversion(new ValueConverter<MessageId, Guid>(id => id.Value, v => MessageId.From(v)))
        .IsRequired(false);

    builder.Property(m => m.IsDelete)
        .IsRequired();

    builder.HasOne(m => m.ParentMessage)
        .WithMany(m => m.Replies)
        .HasForeignKey(m => m.ParentMessageId)
        .OnDelete(DeleteBehavior.Restrict);
  }
}

