using System.Text.Json;
using Microsoft.Extensions.Options;
using PollSurvey.Contracts;
using RabbitMQ.Client;

namespace VotingService.Messaging;

public interface IVoteEventPublisher
{
    Task PublishAsync(VoteRecordedEvent message, CancellationToken cancellationToken);
}

public sealed class RabbitMqVoteEventPublisher : IVoteEventPublisher, IAsyncDisposable
{
    private const string RoutingKey = "vote.recorded";
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqVoteEventPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task PublishAsync(
        VoteRecordedEvent message,
        CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        await _channelLock.WaitAsync(cancellationToken);

        try
        {
            await EnsureConnectedAsync(cancellationToken);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel!.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _channelLock.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return;

        await DisposeConnectionAsync();

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "poll-voting-service"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _channelLock.WaitAsync();
        try
        {
            await DisposeConnectionAsync();
        }
        finally
        {
            _channelLock.Release();
            _channelLock.Dispose();
        }
    }

    private async ValueTask DisposeConnectionAsync()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;

        if (channel is not null)
        {
            try
            {
                if (channel.IsOpen)
                    await channel.CloseAsync();
            }
            finally
            {
                await channel.DisposeAsync();
            }
        }

        if (connection is not null)
        {
            try
            {
                if (connection.IsOpen)
                    await connection.CloseAsync();
            }
            finally
            {
                await connection.DisposeAsync();
            }
        }
    }
}
