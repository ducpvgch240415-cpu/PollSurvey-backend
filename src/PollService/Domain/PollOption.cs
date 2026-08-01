namespace PollService.Domain;

public sealed class PollOption
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public required string Text { get; set; }
    public int Position { get; set; }
    public Poll Poll { get; set; } = null!;
}

