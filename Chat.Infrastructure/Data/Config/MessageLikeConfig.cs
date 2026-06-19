



namespace Chat.Infrastructure.Data.Config;

public class MessageLikeConfig : IEntityTypeConfiguration<MessageLike>
{
  public void Configure(EntityTypeBuilder<MessageLike> builder)
  {
    builder.ToTable("MessageLike");

    builder.HasKey(x => new { x.MessageId, x.UserId });

    builder.Property(x => x.MessageId)
        .HasConversion(id => id.Value, v => MessageId.From(v))
        .IsRequired();

    builder.Property(x => x.UserId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.DateTimeLike)
        .IsRequired();

    builder.Property(x => x.LikeType)
        .HasConversion(t => t.Value, v => LikeType.FromValue(v))
        .IsRequired();

    builder.Property(x => x.IsActive)
        .IsRequired();

    builder.HasOne<Message>()
        .WithMany(m => m.MessageLikes)
        .HasForeignKey(x => x.MessageId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}

