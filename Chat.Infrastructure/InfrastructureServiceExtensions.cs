using Chat.Core.AccountAgg;



using Chat.UseCases.ChatApp;
using Chat.Infrastructure._Repository;
using Chat.Infrastructure._Services;
using Chat.Infrastructure._Services.Kafka;
using Chat.Infrastructure.Data.Context;
using Chat.Infrastructure.HostedBack;
using StackExchange.Redis;

namespace Chat.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    IConfiguration config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("cleanarchitecture")
                               ?? config.GetConnectionString("DefaultConnection");

    Guard.Against.Null(connectionString);

    services.AddScoped<EventDispatchInterceptor>();
    services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();

    services.AddDbContext<ChatDbContext>((provider, options) =>
    {
      var eventDispatchInterceptor = provider.GetRequiredService<EventDispatchInterceptor>();

      if (config.GetConnectionString("cleanarchitecture") != null ||
          config.GetConnectionString("DefaultConnection") != null)
      {
        options.UseSqlServer(connectionString);
      }
      else
      {
        options.UseSqlite(connectionString);
      }

      options.AddInterceptors(eventDispatchInterceptor);
    });

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
            .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

    // Repositories
    services.AddScoped<IMessageRepository, MessageRepository>();
    services.AddScoped<IOperatingStatusRepository, OperatingStatusRepository>();
    services.AddScoped<IProfileUserRepoRedis, ProfileUserRepoRedis>();

    // Services
    services.AddScoped<IChatHubService, ChatHubService>();
    services.AddScoped<IChatQueryService, ChatQueryService>();
    services.AddScoped<IChatCommandService, ChatCommandService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<ICookieService, CookieService>();
    services.AddSingleton<KafkaProducerService>();
    services.AddHostedService<KafkaConsumerService>();

    // Redis
    services.AddSingleton<IConnectionMultiplexer>(sp =>
    {
      var redisConnection = config["Redis:Connection"] ?? "localhost:6379";
      var options = ConfigurationOptions.Parse(redisConnection, true);
      return ConnectionMultiplexer.Connect(options);
    });

    services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = config["Redis:Connection"] ?? "localhost:6379";
      options.InstanceName = "ChatApp_";
    });

    logger.LogInformation("Infrastructure services registered successfully.");

    return services;
  }
}

