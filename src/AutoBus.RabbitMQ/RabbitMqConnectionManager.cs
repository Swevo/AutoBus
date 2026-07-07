using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AutoBus.RabbitMQ;

/// <summary>
/// Owns a single shared <see cref="IConnection"/> to the broker, created lazily on first use.
/// </summary>
public sealed class RabbitMqConnectionManager : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly Lazy<IConnection> _connection;

    public RabbitMqConnectionManager(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
        _connection = new Lazy<IConnection>(CreateConnection);
    }

    /// <summary>Opens a new channel on the shared connection.</summary>
    public IModel CreateModel() => _connection.Value.CreateModel();

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true,
        };

        return factory.CreateConnection();
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value.Dispose();
        }
    }
}
