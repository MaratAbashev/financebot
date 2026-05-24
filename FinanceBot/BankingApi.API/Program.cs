using System.Text;
using BankingApi.API.Middleware;
using BankingApi.API.Validators;
using BankingApi.Application.Interfaces;
using BankingApi.Application.Settings;
using BankingApi.Infrastructure.Services;
using BankingApi.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var services = builder.Services;

services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
services.Configure<VerificationCodeSettings>(builder.Configuration.GetSection("VerificationCode"));
services.Configure<OAuthSettings>(builder.Configuration.GetSection("OAuth"));

services.AddSingleton(TimeProvider.System);

services.AddOpenApi();

services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IJwtService, JwtService>();
services.AddScoped<ISmsService, ConsoleSmsService>();
services.AddScoped<IAccountService, AccountService>();
services.AddScoped<ITransactionService, TransactionService>();
services.AddScoped<ICategoryService, CategoryService>();
services.AddScoped<IOAuthService, OAuthService>();

services.AddValidatorsFromAssemblyContaining<SendCodeRequestValidator>();
services.AddFluentValidationAutoValidation();

services.AddHttpClient();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
    });

services.AddAuthorization();
services.AddControllers();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseStaticFiles();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var service = scope.ServiceProvider;
    try
    {
        var context = service.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        await DatabaseSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = service.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при применении миграций.");
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();