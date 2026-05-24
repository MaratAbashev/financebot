using System.Diagnostics;
using FinBot.Domain.KafkaMessages;
using FinBot.Domain.Options;
using FinBot.Kafka.Abstractions.MessageHandlers;
using FinBot.MinIOS3;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace FinBot.Bll.Implementation.Handlers;

public class ExcelCompletedMessageHandler(ITelegramBotClient botClient, IMinioStorage minioStorage, IOptions<StorageOptions> storageOptions): IMessageHandler<ExcelCompletedMessage>
{
    public async Task HandleAsync(ExcelCompletedMessage message, CancellationToken cancellationToken = default)
    {
        if (message.FileKey is null)
        {
            if (message.IsNoExpensesForPeriod)
            {
                await botClient.SendMessage(
                    message.UserTgId, 
                    "Траты за указанный период не обнаружены", 
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(
                    message.UserTgId, 
                    "При формировании отчета возникла ошибка", 
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            var bucketName = message.FileKey.Split('_').First() switch
            {
                "barChart" => storageOptions.Value.BarChartsBucket,
                "lineChart" => storageOptions.Value.LineChartsBucket,
                _ => throw new ArgumentOutOfRangeException()
            };
            var storageGetResult = await minioStorage.GetAsync(bucketName, message.FileKey, cancellationToken);
            if (storageGetResult.IsSuccess)
            {
                await botClient.SendDocument(
                    message.UserTgId,
                    InputFile.FromStream(new MemoryStream(storageGetResult.Data)),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);
                return;
            }
            await botClient.SendMessage(
                message.UserTgId, 
                "Ваш отчет доступен по ссылке " + message.FileKey, 
                cancellationToken: cancellationToken);
        }
    }
}