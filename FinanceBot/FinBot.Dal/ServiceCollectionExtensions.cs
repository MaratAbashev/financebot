using FinBot.Bll.Interfaces;
using FinBot.Dal.DbContexts;
using FinBot.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinBot.Dal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString(nameof(PDbContext)));
            options.UseSnakeCaseNamingConvention();
        });
        
        services.AddScoped<IGenericRepository<DialogContext, int, PDbContext>, GenericRepository<DialogContext, int, PDbContext>>();
        services.AddScoped<IGenericRepository<User, Guid, PDbContext>, GenericRepository<User, Guid, PDbContext>>();
        services.AddScoped<IGenericRepository<Group, Guid, PDbContext>, GenericRepository<Group, Guid, PDbContext>>();
        services.AddScoped<IGenericRepository<Saving, Guid, PDbContext>, GenericRepository<Saving, Guid, PDbContext>>();
        
        services.AddScoped<IUnitOfWork<PDbContext>, UnitOfWork<PDbContext>>();
        
        return services;
    }
}