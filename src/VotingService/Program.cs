using Microsoft.EntityFrameworkCore;
using VotingService.Clients;
using VotingService.Data;
using VotingService.Domain;
using VotingService.Messaging;

var builder = WebApplication.CreateBuilder(args);
var voteConnectionString = builder.Configuration.GetConnectionString("VoteDb");

if (string.IsNullOrWhiteSpace(voteConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:VoteDb is required. Set VOTE_DB_CONNECTION_STRING in .env.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<VoteDbContext>(options =>
    options.UseNpgsql(voteConnectionString));
builder.Services.AddHttpClient<IPollServiceClient, PollServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["PollService:BaseUrl"]
        ?? "http://localhost:8081/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<IVoterTokenProtector, VoterTokenProtector>();
builder.Services.AddSingleton<IVoteEventPublisher, RabbitMqVoteEventPublisher>();

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
            var database = scope.ServiceProvider.GetRequiredService<VoteDbContext>();
            await database.Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception exception) when (attempt < 10)
        {
            app.Logger.LogWarning(exception,
                "Vote database is unavailable. Retry {Attempt}/10.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program { }
