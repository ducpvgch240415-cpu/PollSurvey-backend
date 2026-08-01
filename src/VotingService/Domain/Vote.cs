namespace VotingService.Domain;

public sealed class Vote
{
    public Guid Id { get; set; }
    public required string PollCode { get; set; }
    public Guid OptionId { get; set; }
    public required string VoterTokenHash { get; set; }
    public DateTimeOffset VotedAt { get; set; }
}

