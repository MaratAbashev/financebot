using FinBot.BankService;
using FinBot.BankService.Hangfire;
using FinBot.BankService.Models;
using FinBot.BankService.Repositories;
using FinBot.BankService.Services;
using FinBot.Observability;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddObservability(configuration);

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services
    .AddBankServicePipeline(configuration);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

// Hangfire — синхронизация каждый час
using var scope = app.Services.CreateScope();
var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

recurringJobManager.AddOrUpdate<BankSyncJob>(
    "bank-sync",
    job => job.ExecuteAsync(),
    "0 * * * *"
);

// OAuth callback — Banking API сюда шлёт токены
app.MapPost("/oauth/callback", async (
    [FromBody] OAuthCallbackPayload payload,
    IBankAuthService authService,
    CancellationToken ct) =>
{
    await authService.HandleCallbackAsync(payload, ct);
    return Results.Ok();
});

// Получить ссылку для привязки банка
app.MapGet("/bank/auth-url/{userId:guid}", async (
    [FromRoute] Guid userId,
    IBankAuthService authService,
    CancellationToken ct) =>
{
    var result = await authService.GetAuthUrlAsync(userId, ct);
    return result.IsSuccess
        ? Results.Ok(result.Data)
        : Results.Problem(result.ErrorMessage);
});

app.MapGet("/bank/auth/unlink/{userId:guid}", async (
    [FromRoute] Guid userId,
    IBankAuthService authService,
    CancellationToken ct) =>
{
    try
    {
        await authService.UnlinkBankAsync(userId, ct);
        return Results.Ok("Отвязка прошла успешно!");
    }
    catch (Exception)
    {
        return Results.Problem("Не удалось сделать отвязку");
    }
});

// Получить новые транзакции пользователя
app.MapGet("/bank/transactions/pending/{userId:guid}", async (
    [FromRoute] Guid userId,
    IBankTransactionRepository transactions,
    CancellationToken ct) =>
{
    var result = await transactions.GetPendingByUserIdAsync(userId, ct);
    return Results.Ok(result);
});

app.MapGet("/bank/transactions/sync/{userId:guid}", async (
    [FromRoute] Guid userId,
    IBankSyncService syncService,
    CancellationToken ct) =>
{
    var result = await syncService.SyncUserAsync(userId, ct);
    return Results.Ok(result);
});

app.Run();