namespace PollService.DTOs;

public sealed record CreatePollRequest(string Question, IReadOnlyList<string> Options);

public sealed record CreatePollResponse(
    string Code,
    string CreatorToken,
    string SharePath,
    DateTimeOffset CreatedAt);

public sealed record UpdatePollRequest(
    string? CreatorToken,
    string Question,
    IReadOnlyList<string> Options);

public sealed record PollOptionResponse(Guid Id, string Text, int Position);

public sealed record PollResponse(
    string Code,
    string Question,
    bool IsClosed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<PollOptionResponse> Options);

public sealed record ClosePollRequest(string? CreatorToken);
