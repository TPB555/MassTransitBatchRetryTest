using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MassTransitBatchRetryTest;

public class MassTransitConsoleHostedService : IHostedService
{
    private readonly IBusControl _bus;
    private readonly ILogger _logger;

    public MassTransitConsoleHostedService(IBusControl bus, ILogger<MassTransitConsoleHostedService> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting bus");
        await _bus.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping bus");
        await _bus.StopAsync(cancellationToken);
    }
}