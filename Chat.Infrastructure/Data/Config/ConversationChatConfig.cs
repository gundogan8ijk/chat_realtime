


namespace Chat.Infrastructure.Data.Config;

public class ConversationChatConfig : IEntityTypeConfiguration<ConversationChat>
{
  public void Configure(EntityTypeBuilder<ConversationChat> builder)
  {
    builder.ToTable("ConversationChat");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .HasConversion(id => id.Value, v => ConversationId.From(v));

    builder.Property(x => x.LastSentMessageId)
        .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, v => v == null ? null : MessageId.From(v.Value))
        .IsRequired(false);

    builder.Property(x => x.UserBlockId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired(false); // Make it optional in EF if it is default empty

    builder.HasOne<Message>()
        .WithOne()
        .HasForeignKey<ConversationChat>(x => x.LastSentMessageId)
        .OnDelete(DeleteBehavior.SetNull);

    builder.Navigation(x => x.UserConversations)
        .HasField("_userConversations")
        .UsePropertyAccessMode(PropertyAccessMode.Field);
  }
}

