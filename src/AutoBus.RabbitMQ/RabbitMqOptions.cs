namespace AutoBus.RabbitMQ;

/// <summary>
/// Connection and topology settings for the RabbitMQ transport.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>RabbitMQ host name. Default "localhost".</summary>
    public string HostName { get; set; } = "localhost";

    /// <summary>RabbitMQ port. Default 5672.</summary>
    public int Port { get; set; } = 5672;

    /// <summary>RabbitMQ virtual host. Default "/".</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>RabbitMQ user name. Default "guest".</summary>
    public string UserName { get; set; } = "guest";

    /// <summary>RabbitMQ password. Default "guest".</summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Prefix applied to queue names, so multiple services sharing a broker don't collide.
    /// Default "autobus".
    /// </summary>
    public string QueuePrefix { get; set; } = "autobus";
}
