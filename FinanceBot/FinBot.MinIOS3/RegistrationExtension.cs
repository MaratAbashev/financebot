using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;

namespace FinBot.MinIOS3;

public static class RegistrationExtension
{
    public static IServiceCollection AddMinioS3(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "MinioOptions",
        bool addInitializer = true)
    {
        services.Configure<MinioOptions>(configuration.GetSection(sectionName));

        services.AddSingleton<IMinioClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MinioOptions>>().Value;
            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithTimeout(30000)
                .Build();
        });

        services.AddSingleton<IMinioStorage, MinioStorage>();

        if (addInitializer)
            services.AddHostedService<MinioInitializer>();

        return services;
    }
}