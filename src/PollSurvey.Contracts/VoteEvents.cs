namespace PollSurvey.Contracts;

public sealed record PollOptionResult(
    Guid OptionId,
    string Text,
    int Position,
    int Votes);

public sealed record PollResults(
    string Code,
    int TotalVotes,
    IReadOnlyList<PollOptionResult> Options);

public sealed record VoteRecordedEvent(
    string PollCode,
    DateTimeOffset RecordedAt,
    PollResults Results);

