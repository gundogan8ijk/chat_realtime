



using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chat.Infrastructure.Data.Config;

public class GroupChatConfig : IEntityTypeConfiguration<GroupChat>
{
  public void Configure(EntityTypeBuilder<GroupChat> builder)
  {
    builder.ToTable("GroupChat");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .HasConversion(new ValueConverter<GroupChatId, Guid>(id => id.Value, v => GroupChatId.From(v)));

    builder.Property(x => x.Name)
        .HasConversion(new ValueConverter<GroupName, string>(n => n.Value, v => GroupName.From(v)))
        .HasMaxLength(200)
        .IsRequired();

    builder.Property(x => x.CreateDate)
        .IsRequired();

    builder.Property(x => x.IsActive)
        .IsRequired();

    builder.Property(x => x.AvatarUrl)
        .HasConversion(new ValueConverter<AvatarUrl, string>(a => a.Value, v => AvatarUrl.From(v)))
        .HasMaxLength(500)
        .IsRequired(false);

    builder.Property(x => x.IsPrivate)
        .IsRequired();

    builder.Property(x => x.LastSentMessageId)
        .HasConversion(new ValueConverter<MessageId, Guid>(id => id.Value, v => MessageId.From(v)))
        .IsRequired(false);

    builder.HasOne<Message>()
        .WithOne()
        .HasForeignKey<GroupChat>(x => x.LastSentMessageId)
        .OnDelete(DeleteBehavior.SetNull);
  }
}

