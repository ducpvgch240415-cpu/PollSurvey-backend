namespace VotingService.DTOs;

public sealed record SubmitVoteRequest(Guid OptionId);

public sealed record RecentVote(
    Guid OptionId,
    string OptionText,
    int OptionPosition,
    DateTimeOffset VotedAt);

public sealed record RecentVotesResponse(
    string PollCode,
    int TotalVotes,
    IReadOnlyList<RecentVote> Votes);