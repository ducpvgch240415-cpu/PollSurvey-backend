namespace PollService.Domain;

public sealed class Poll
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Question { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public required string CreatorTokenHash { get; set; }
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
}
