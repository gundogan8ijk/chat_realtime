


namespace Chat.Infrastructure.Data.Config;

public class UserConversationConfig : IEntityTypeConfiguration<UserConversation>
{
  public void Configure(EntityTypeBuilder<UserConversation> builder)
  {
    builder.ToTable("UserConversation");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .ValueGeneratedNever();

    builder.Property(x => x.UserId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.PartnerId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.ConversationId)
        .HasConversion(id => id.Value, v => ConversationId.From(v))
        .IsRequired();

    builder.Property(x => x.LastReadMessageId)
        .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, v => v == null ? null : MessageId.From(v.Value))
        .IsRequired(false);

    builder.HasOne<ConversationChat>()
        .WithMany(y => y.UserConversations)
        .HasForeignKey(x => x.ConversationId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasOne<Message>()
        .WithOne()
        .HasForeignKey<UserConversation>(x => x.LastReadMessageId)
        .OnDelete(DeleteBehavior.SetNull);

    // Unique indexes
    builder.HasIndex("UserId", "PartnerId")
        .IsUnique();

    builder.HasIndex("UserId", "ConversationId")
        .IsUnique();

    builder.HasIndex("PartnerId", "ConversationId")
        .IsUnique();
  }
}

