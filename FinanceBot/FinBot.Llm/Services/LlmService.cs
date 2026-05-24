using System.Collections.Generic;
using FinBot.Domain.Models.Enums;
using FinBot.Domain.Reports;
using FinBot.Llm.Repositories;
using FinBot.Observability.Metrics;
using LD.Sber.GigaChatSDK.Interfaces;
using LD.Sber.GigaChatSDK.Models;

namespace FinBot.Llm.Services;

public class LlmService(
    IExpenseRepository repository,
    TimeProvider timeProvider,
    IGigaChat gigaChat,
    BusinessMetrics businessMetrics,
    ILogger<LlmService> logger) : ILlmService
{
    public async Task<string> GetAnalysisAsync(long userTgId, Guid groupId, TimeInterval timeInterval,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "GetAnalysisAsync called for UserId={UserId}, GroupId={GroupId}, TimeInterval={TimeInterval}",
            userTgId, groupId, timeInterval);
 
        return await DoLlmRoutine(
            userTgId, groupId,
            "вот список моих трат с датами и категориями, дай сводку-аналитику о том как я трачу деньги,",
            timeInterval, cancellationToken);
    }
 
    public async Task<string> GetAdviceAsync(long userTgId, Guid groupId, TimeInterval timeInterval,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "GetAdviceAsync called for UserId={UserId}, GroupId={GroupId}, TimeInterval={TimeInterval}",
            userTgId, groupId, timeInterval);
 
        return await DoLlmRoutine(
            userTgId, groupId,
            "вот список моих трат с датами и категориями, проанализируй и дай советы по экономии",
            timeInterval, cancellationToken);
    }
 
    private async Task<string> DoLlmRoutine(long userTgId, Guid groupId, string prompt,
        TimeInterval timeInterval,
        CancellationToken cancellationToken = default)
    {
        var period = PeriodCalculator.ForPrevious(timeInterval, timeProvider.GetUtcNow());
 
        logger.LogDebug(
            "Fetching expenses for UserId={UserId}, GroupId={GroupId}, Period={PeriodFrom:O} - {PeriodTo:O}",
            userTgId, groupId, period.From, period.To);
 
        var expenses = await repository.GetExpensesForUserInGroupAsync(
            userTgId, groupId, period.From, period.To, cancellationToken: cancellationToken);
 
        if (expenses.Count == 0)
        {
            logger.LogInformation(
                "No expenses found for UserId={UserId}, GroupId={GroupId}, TimeInterval={TimeInterval}",
                userTgId, groupId, timeInterval);

            businessMetrics.LlmOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", "not_found"));

            return "У вас нет трат за заданный период";
        }
 
        logger.LogInformation(
            "Fetched {ExpenseCount} expenses for UserId={UserId}, GroupId={GroupId}, TimeInterval={TimeInterval}. Sending to GigaChat",
            expenses.Count, userTgId, groupId, timeInterval);
 
        var stringExpenses = string.Join(';',
            expenses.Select(e => $"{e.Amount},{e.Category.ToString()},{e.Date.ToShortDateString()}"));
 
        var messageQuery = new MessageQuery();
        messageQuery.Messages.Add(new MessageContent("user", prompt + stringExpenses));
 
        var sw = System.Diagnostics.Stopwatch.StartNew();
        Response? response = await gigaChat.CompletionsAsync(messageQuery);
        sw.Stop();
 
        var content = response?.Choices?.LastOrDefault()?.Message?.Content;
 
        if (content == null)
        {
            logger.LogWarning(
                "GigaChat returned empty response for UserId={UserId}, GroupId={GroupId}, ElapsedMs={ElapsedMs}",
                userTgId, groupId, sw.ElapsedMilliseconds);

            businessMetrics.LlmOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", "error"));

            return "Ответа не поступило";
        }

        logger.LogInformation(
            "GigaChat response received for UserId={UserId}, GroupId={GroupId}, ElapsedMs={ElapsedMs}, ResponseLength={ResponseLength}",
            userTgId, groupId, sw.ElapsedMilliseconds, content.Length);

        businessMetrics.LlmOutcomeTotal.Add(1, new KeyValuePair<string, object?>("outcome", "generated"));

        return content;
    }
}