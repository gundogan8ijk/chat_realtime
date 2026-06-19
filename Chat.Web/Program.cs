using Chat.Infrastructure;
using Chat.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog or default console logger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger<Program>();
logger.LogInformation("Starting Chat Web Host");

// Add Clean Architecture project layers
builder.Services.AddInfrastructureServices(builder.Configuration, logger);
builder.Services.AddWebApiServices(builder.Configuration);

// Add mediator source generator (for source generator support)
builder.Services.AddMediator();

var app = builder.Build();

app.UseWebApiServices();

app.Run();

public partial class Program { }

