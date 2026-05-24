using BankingApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BankingApi.Infrastructure.Services;

public class ConsoleSmsService(ILogger<ConsoleSmsService> logger) : ISmsService
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        logger.LogWarning("SMS to {Phone}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}