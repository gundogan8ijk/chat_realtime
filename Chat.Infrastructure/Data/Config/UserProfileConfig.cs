using Chat.Core.AccountAgg;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Data.Config;

public class UserProfileConfig : IEntityTypeConfiguration<UserProfile>
{
  public void Configure(EntityTypeBuilder<UserProfile> builder)
  {
    builder.ToTable("UserProfile");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .ValueGeneratedNever();

    builder.Property(x => x.FirstName)
        .HasConversion(id => id.Value, v => FirstName.From(v))
        .HasMaxLength(100)
        .IsRequired();

    builder.Property(x => x.LastName)
        .HasConversion(id => id.Value, v => LastName.From(v))
        .HasMaxLength(100)
        .IsRequired();

    builder.Property(x => x.AvatarUrl)
        .HasConversion(
            id => id == null ? null : id.Value.Value,
            v => v == null ? null : AvatarUrl.From(v)
        )
        .HasMaxLength(500)
        .IsRequired(false);

    builder.Property(x => x.CreateDate)
        .IsRequired();
  }
}

