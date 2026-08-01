using Microsoft.EntityFrameworkCore;
using PollService.Data;
using PollService.Services;

var builder = WebApplication.CreateBuilder(args);
var pollConnectionString = builder.Configuration.GetConnectionString("PollDb");

if (string.IsNullOrWhiteSpace(pollConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:PollDb is required. Set POLL_DB_CONNECTION_STRING in .env.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<PollDbContext>(options =>
    options.UseNpgsql(pollConnectionString));
builder.Services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
builder.Services.AddSingleton<ICreatorTokenProtector, CreatorTokenProtector>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.MapHealthChecks("/health");

await InitialiseDatabase(app);
app.Run();

static async Task InitialiseDatabase(WebApplication app)
{
    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<PollDbContext>();
            await database.Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception exception) when (attempt < 10)
        {
            app.Logger.LogWarning(exception,
                "Poll database is unavailable. Retry {Attempt}/10.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program { }
