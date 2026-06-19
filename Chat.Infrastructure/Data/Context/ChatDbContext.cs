using Chat.Core.AccountAgg;


using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chat.Infrastructure.Data.Context;

public class ChatDbContext(DbContextOptions<ChatDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
  public DbSet<Message> Messages => Set<Message>();
  public DbSet<ConversationChat> ConversationChats => Set<ConversationChat>();
  public DbSet<RoomMessage> RoomMessages => Set<RoomMessage>();
  public DbSet<UserConversation> UserConversations => Set<UserConversation>();
  public DbSet<MessageLike> MessageLikes => Set<MessageLike>();
  public DbSet<Friendship> Friendships => Set<Friendship>();
  public DbSet<GroupChat> GroupChats => Set<GroupChat>();
  public DbSet<UserGroup> UserGroups => Set<UserGroup>();
  public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
  public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.ConfigureWarnings(w =>
        w.Ignore(RelationalEventId.PendingModelChangesWarning));
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
  }

  public override int SaveChanges() =>
        SaveChangesAsync().GetAwaiter().GetResult();
}

