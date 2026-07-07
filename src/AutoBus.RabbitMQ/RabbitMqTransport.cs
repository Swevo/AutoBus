using System.Text.Json;
using RabbitMQ.Client;

namespace AutoBus.RabbitMQ;

/// <summary>
/// Publishes messages to a per-message-type fanout exchange on RabbitMQ.
/// </summary>
public sealed class RabbitMqTransport : IBusTransport, IDisposable
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly Lazy<IModel> _model;

    public RabbitMqTransport(RabbitMqConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        _model = new Lazy<IModel>(_connectionManager.CreateModel);
    }

    /// <inheritdoc />
    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        var exchange = RabbitMqTopology.ExchangeNameFor(messageType);
        var model = _model.Value;

        model.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, messageType);
        var properties = model.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.Type = messageType.AssemblyQualifiedName ?? messageType.FullName ?? messageType.Name;
        properties.Persistent = true;

        model.BasicPublish(exchange, routingKey: string.Empty, properties, body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_model.IsValueCreated)
        {
            _model.Value.Dispose();
        }
    }
}
