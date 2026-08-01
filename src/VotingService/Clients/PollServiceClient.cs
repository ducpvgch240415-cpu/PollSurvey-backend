using System.Net;
using System.Net.Http.Json;

namespace VotingService.Clients;

public sealed record PollOptionContract(Guid Id, string Text, int Position);

public sealed record PollContract(
    string Code,
    string Question,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PollOptionContract> Options);

public interface IPollServiceClient
{
    Task<PollContract?> GetPollAsync(string code, CancellationToken cancellationToken);
}

public sealed class PollServiceClient(HttpClient httpClient) : IPollServiceClient
{
    public async Task<PollContract?> GetPollAsync(
        string code,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"polls/{Uri.EscapeDataString(code)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PollContract>(cancellationToken);
    }
}

