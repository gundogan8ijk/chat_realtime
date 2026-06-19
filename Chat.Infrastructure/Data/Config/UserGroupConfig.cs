



namespace Chat.Infrastructure.Data.Config;

public class UserGroupConfig : IEntityTypeConfiguration<UserGroup>
{
  public void Configure(EntityTypeBuilder<UserGroup> builder)
  {
    builder.ToTable("UserGroup");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .HasConversion(id => id.Value, v => UserGroupId.From(v));

    builder.Property(x => x.GroupChatId)
        .HasConversion(id => id.Value, v => GroupChatId.From(v))
        .IsRequired();

    builder.Property(x => x.UserId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.IsAdmin)
        .IsRequired();

    builder.Property(x => x.JoinedAt)
        .IsRequired();

    builder.Property(x => x.LastReadMessageId)
        .HasConversion(id => id == null ? (Guid?)null : id.Value.Value, v => v == null ? null : MessageId.From(v.Value))
        .IsRequired(false);

    builder.HasOne<GroupChat>()
        .WithMany(y => y.UserGroups)
        .HasForeignKey(x => x.GroupChatId)
        .OnDelete(DeleteBehavior.Cascade);

    builder.HasIndex(x => x.UserId);
  }
}

