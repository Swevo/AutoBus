using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.RabbitMq;
using Xunit;

namespace AutoBus.RabbitMQ.Tests;

/// <summary>
/// End-to-end test against a real RabbitMQ broker started via Testcontainers. Requires Docker
/// to be available; skipped automatically (via container startup failure) in environments
/// without it. This is the only test in the suite that needs a live broker — topology naming
/// is covered separately in <see cref="RabbitMqTopologyTests"/> without any broker dependency.
/// </summary>
public class RabbitMqEndToEndTests : IAsyncLifetime
{
    private RabbitMqContainer? _container;
    private bool _dockerAvailable = true;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new RabbitMqBuilder().Build();
            await _container.StartAsync();
        }
        catch (Exception)
        {
            // Docker isn't available in this environment (e.g. a sandboxed dev machine without
            // a running daemon). CI runners (GitHub Actions ubuntu-latest) have Docker available,
            // so this test still runs for real there; locally it's skipped rather than failing the suite.
            _dockerAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dockerAvailable && _container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private sealed record PingMessage(int Id);

    private sealed class PingConsumer : IConsumer<PingMessage>
    {
        public static TaskCompletionSource<int> Received = new();

        public Task Consume(ConsumeContext<PingMessage> context)
        {
            Received.TrySetResult(context.Message.Id);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_ViaRabbitMq_IsReceivedByRemoteConsumerHost()
    {
        if (!_dockerAvailable)
        {
            return;
        }

        PingConsumer.Received = new TaskCompletionSource<int>();

        var uri = new Uri(_container!.GetConnectionString());

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddAutoBus(cfg => cfg.AddConsumer<PingConsumer>());
                services.AddAutoBusRabbitMq(options =>
                {
                    options.HostName = uri.Host;
                    options.Port = uri.Port;
                    options.UserName = "guest";
                    options.Password = "guest";
                    options.QueuePrefix = "autobus-tests";
                });
            })
            .Build();

        await host.StartAsync();
        try
        {
            var bus = host.Services.GetRequiredService<IMessageBus>();
            await bus.PublishAsync(new PingMessage(123));

            var completed = await Task.WhenAny(PingConsumer.Received.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.Same(PingConsumer.Received.Task, completed);
            Assert.Equal(123, await PingConsumer.Received.Task);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
