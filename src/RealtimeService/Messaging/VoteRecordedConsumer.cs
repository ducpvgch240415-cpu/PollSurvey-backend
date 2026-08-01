using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PollSurvey.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RealtimeService.Hubs;

namespace RealtimeService.Messaging;

public sealed class VoteRecordedConsumer(
    IOptions<RabbitMqOptions> options,
    IHubContext<PollHub> hubContext,
    ILogger<VoteRecordedConsumer> logger) : BackgroundService
{
    private const string RoutingKey = "vote.recorded";
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(stoppingToken);
                await StartConsumerAsync(stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "RabbitMQ consumer failed. Reconnecting in 5 seconds.");
                await DisposeConnectionAsync();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            ClientProvidedName = "poll-realtime-service"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: RoutingKey,
            cancellationToken: cancellationToken);
        await _channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 20,
            global: false,
            cancellationToken: cancellationToken);
    }

    private async Task StartConsumerAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<VoteRecordedEvent>(eventArgs.Body.Span);
                if (message is null)
                    throw new JsonException("The vote event was empty.");

                await hubContext.Clients
                    .Group(PollHub.GroupName(message.PollCode))
                    .SendAsync("ResultsUpdated", message.Results, stoppingToken);

                await _channel!.BasicAckAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "A vote event could not be processed.");
                await _channel!.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: stoppingToken);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await DisposeConnectionAsync();
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
