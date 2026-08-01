namespace VotingService.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "polluser";
    public string Password { get; set; } = "pollpass";
    public string ExchangeName { get; set; } = "poll.events";
}
