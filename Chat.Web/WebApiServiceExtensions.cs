using System.Text;
using Chat.Infrastructure.Hubs;
using Chat.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FastEndpoints.Swagger;

namespace Chat.Web;

public static class WebApiServiceExtensions
{
  public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<JWTOptions>()
        .Bind(configuration.GetSection(JWTOptions.JWTOptionsKey))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    var jwt = configuration.GetSection(JWTOptions.JWTOptionsKey).Get<JWTOptions>();

    var authBuilder = services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt?.Issuer ?? "ChatApi",
        ValidAudience = jwt?.Audience ?? "ChatClient",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt?.Secret ?? throw new InvalidOperationException("JWTOptions:Secret is missing from configuration."))
        ),
        ClockSkew = TimeSpan.Zero
      };

      options.Events = new JwtBearerEvents
      {
        OnMessageReceived = context =>
        {
          // Check query string first for SignalR
          var accessToken = context.Request.Query["access_token"];
          var path = context.HttpContext.Request.Path;
          if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
          {
            context.Token = accessToken;
          }
          else
          {
            // Fallback to cookie
            context.Token = context.Request.Cookies["AccessToken"];
          }
          return Task.CompletedTask;
        }
      };
    });

    var googleClientId = configuration["GoogleAuth:ClientId"];
    var googleClientSecret = configuration["GoogleAuth:ClientSecret"];
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
    {
      authBuilder.AddGoogle(options =>
      {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SaveTokens = true;
      });
    }

    // FastEndpoints & Swagger
    services.AddFastEndpoints();
    services.SwaggerDocument();

    services.AddSignalR();
    services.AddHttpContextAccessor();

    services.AddCors(options =>
    {
      options.AddPolicy("AllowFrontend", policy =>
      {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
      });
    });

    services.AddAuthorization();
    services.AddProblemDetails();

    return services;
  }

  public static WebApplication UseWebApiServices(this WebApplication app)
  {
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseFastEndpoints();
    
    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen();
    }

    app.MapHub<ChatHub>("/chatHub");

    return app;
  }
}

