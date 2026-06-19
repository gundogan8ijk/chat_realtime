




namespace Chat.Infrastructure.Data.Config;

public class FriendshipConfig : IEntityTypeConfiguration<Friendship>
{
  public void Configure(EntityTypeBuilder<Friendship> builder)
  {
    builder.ToTable("Friendship");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .HasConversion(id => id.Value, v => FriendshipId.From(v));

    builder.Property(x => x.UserId_A)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.UserId_B)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.Status)
        .HasConversion(s => s.Value, v => FriendStatus.FromValue(v))
        .IsRequired();

    builder.Property(x => x.CreatedAt)
        .IsRequired();

    // Unique index for the pair of users (so they can't have duplicate friendship rows)
    // In EF Core, unique index requires custom columns or we can just use the mapped properties
    builder.HasIndex("UserId_A", "UserId_B")
        .IsUnique();
  }
}

