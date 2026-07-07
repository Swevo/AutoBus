using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AutoBus.RabbitMQ;

/// <summary>
/// Background service that, for every message type with at least one registered AutoBus
/// consumer, declares a durable queue bound to that message type's fanout exchange and
/// dispatches incoming deliveries to <see cref="ConsumerDispatcher"/>.
/// </summary>
public sealed class RabbitMqConsumerHost : BackgroundService
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly ConsumerRegistry _registry;
    private readonly ConsumerDispatcher _dispatcher;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqConsumerHost> _logger;
    private IModel? _model;

    public RabbitMqConsumerHost(
        RabbitMqConnectionManager connectionManager,
        ConsumerRegistry registry,
        ConsumerDispatcher dispatcher,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqConsumerHost> logger)
    {
        _connectionManager = connectionManager;
        _registry = registry;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messageTypes = _registry.RegisteredMessageTypes;
        if (messageTypes.Count == 0)
        {
            _logger.LogInformation("No AutoBus consumers registered; RabbitMQ consumer host has nothing to bind.");
            return Task.CompletedTask;
        }

        _model = _connectionManager.CreateModel();

        foreach (var messageType in messageTypes)
        {
            BindConsumer(_model, messageType, stoppingToken);
        }

        return Task.CompletedTask;
    }

    private void BindConsumer(IModel model, Type messageType, CancellationToken stoppingToken)
    {
        var exchange = RabbitMqTopology.ExchangeNameFor(messageType);
        var queue = RabbitMqTopology.QueueNameFor(messageType, _options.QueuePrefix);

        model.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        model.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false);
        model.QueueBind(queue, exchange, routingKey: string.Empty);

        var consumer = new AsyncEventingBasicConsumer(model);
        consumer.Received += async (_, args) =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize(args.Body.Span, messageType);
                if (payload is null)
                {
                    _logger.LogWarning("RabbitMQ message for {MessageType} deserialized to null. Rejecting without requeue.", messageType.Name);
                    model.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                await _dispatcher.DispatchAsync(payload, messageType, stoppingToken).ConfigureAwait(false);
                model.BasicAck(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch RabbitMQ message for {MessageType}; requeuing.", messageType.Name);
                model.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        model.BasicConsume(queue, autoAck: false, consumer);
    }

    public override void Dispose()
    {
        _model?.Dispose();
        base.Dispose();
    }
}
