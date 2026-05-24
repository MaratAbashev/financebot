using FinBot.Bll.Implementation.Services;

namespace FinBot.WebApi.BackgroundServices;

public class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Polling started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var receiver = scope.ServiceProvider.GetRequiredService<ReceiverService>();
                await receiver.ReceiveAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Polling error, restarting in 3s...");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}