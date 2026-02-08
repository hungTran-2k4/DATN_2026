using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Interfaces.Auth;

namespace MyProject.Infrastructure.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromDays(15);

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupService is running.");

        using var timer = new PeriodicTimer(_period);
        
        // Run immediately once on startup? Or wait? 
        // "every 15 days" usually implies an interval. 
        // Safe bet: Run on start to ensure DB is clean, then wait.
        try
        {
            await DoWorkAsync(stoppingToken);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error occurred during initial token cleanup.");
        }

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token cleanup.");
            }
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenCleanupService executing cleanup...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();
            await repository.RemoveExpiredTokensAsync(stoppingToken);
        }

        _logger.LogInformation("TokenCleanupService cleanup completed.");
    }
}
