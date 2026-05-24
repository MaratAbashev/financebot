using FinBot.Bll.Interfaces.Services;
using FinBot.WebApi.HttpClients;
using IBankServiceClient = FinBot.Bll.Interfaces.Integration.IBankServiceClient;

namespace FinBot.WebApi.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        var appUrl = configuration["App:App"]
                     ?? throw new InvalidOperationException("App:App is not configured");

        var bankServiceUrl = configuration["App:BankService"]
                             ?? throw new InvalidOperationException("App:BankService is not configured");

        services.AddHttpClient<IUserService, HttpUserService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IGroupService, HttpGroupService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IExpenseService, HttpExpenseService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IInvitationService, HttpInvitationService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IGroupBackgroundService, HttpGroupBackgroundService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IInternalBankService, BankInternalHttpService>(c => c.BaseAddress = new Uri(appUrl));
        services.AddHttpClient<IBankServiceClient, BankServiceClient>(c => c.BaseAddress = new Uri(bankServiceUrl));


        return services;
    }
}