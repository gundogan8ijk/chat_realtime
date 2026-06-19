


namespace Chat.Infrastructure.Data.Config;

public class RoomMessageConfig : IEntityTypeConfiguration<RoomMessage>
{
  public void Configure(EntityTypeBuilder<RoomMessage> builder)
  {
    builder.ToTable("RoomMessage");

    builder.HasKey(x => x.RoomId);
    builder.Property(x => x.RoomId)
        .HasConversion(id => id.Value, v => RoomId.From(v));

    builder.Property(x => x.UserId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.ConversationChatId)
        .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, v => v == null ? null : ConversationId.From(v.Value))
        .IsRequired(false);

    builder.Property(x => x.UserConversationId)
        .IsRequired(false);

    builder.Property(x => x.GroupChatId)
        .IsRequired(false);

    builder.Property(x => x.UserGroupId)
        .IsRequired(false);

    builder.HasOne<UserConversation>()
        .WithMany(y => y.RoomUserConversation)
        .HasForeignKey(x => x.UserConversationId)
        .OnDelete(DeleteBehavior.NoAction);

    builder.HasIndex("UserId");
  }
}

