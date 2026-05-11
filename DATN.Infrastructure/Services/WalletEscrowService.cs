using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using DATN.Domain.Interfaces;

namespace DATN.Infrastructure.Services;

public class WalletEscrowService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WalletEscrowService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromHours(1);

    public WalletEscrowService(IServiceProvider serviceProvider, ILogger<WalletEscrowService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WalletEscrowService is starting.");

        using var timer = new PeriodicTimer(_period);

        // Run once on startup
        await DoWorkAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during wallet escrow release.");
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("WalletEscrowService checking for funds to release...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var walletRepo = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            
            // Release funds for orders delivered more than 7 days ago
            // For now, this calls a repository method that handles the complex query
            await walletRepo.ProcessEscrowReleaseAsync(stoppingToken);
        }

        _logger.LogInformation("WalletEscrowService check completed.");
    }
}
