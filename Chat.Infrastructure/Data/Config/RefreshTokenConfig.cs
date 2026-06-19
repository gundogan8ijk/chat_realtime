using Chat.Core.AccountAgg;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Data.Config;

public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
{
  public void Configure(EntityTypeBuilder<RefreshToken> builder)
  {
    builder.ToTable("RefreshToken");

    builder.HasKey(x => x.Id);
    builder.Property(x => x.Id)
        .ValueGeneratedNever();

    builder.Property(x => x.JwtId)
        .HasMaxLength(150)
        .IsRequired();

    builder.Property(x => x.Token)
        .HasMaxLength(250)
        .IsRequired();

    builder.Property(x => x.DeviceId)
        .HasConversion(id => id.Value, v => DeviceId.From(v))
        .HasMaxLength(250)
        .IsRequired();

    builder.Property(x => x.UserId)
        .HasConversion(id => id.Value, v => UserId.From(v))
        .IsRequired();

    builder.Property(x => x.IsUsed)
        .IsRequired();

    builder.Property(x => x.IsRevoked)
        .IsRequired();

    builder.Property(x => x.IssuedAt)
        .IsRequired();

    builder.Property(x => x.ExpireAt)
        .IsRequired();

    // Relationship with ApplicationUser
    builder.HasOne<ApplicationUser>()
        .WithMany()
        .HasForeignKey(x => x.UserId)
        .OnDelete(DeleteBehavior.Cascade);
  }
}

