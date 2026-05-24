using FinBot.Llm.Repositories;
using FinBot.Llm.Services;
using LD.Sber.GigaChatSDK;
using LD.Sber.GigaChatSDK.Interfaces;

namespace FinBot.Llm;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLlmIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IGigaChat, GigaChat>(_ =>
        {
            var token = configuration["Llm:Token"];
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));
            IHttpService httpService = new HttpService(ignoreTLS: true);
            ITokenService tokenService = new TokenService(httpService, token, isCommercial: false);
            return new GigaChat(tokenService, httpService, saveImage: false);
        });
        
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ILlmService, LlmService>();
        
        if (services.All(d => d.ServiceType != typeof(TimeProvider))) 
            services.AddSingleton(TimeProvider.System);
        
        
        return services;
    }
}