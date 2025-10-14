using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace StudentUnionBot.Presentation.Bot.Helpers;

/// <summary>
/// Helper для відображення Loading States та Skeleton Screens
/// Покращує UX показуючи користувачу, що запит обробляється
/// </summary>
public static class LoadingStateHelper
{
    /// <summary>
    /// Показати typing indicator (бот "друкує")
    /// </summary>
    public static async Task ShowTypingAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken = default)
    {
        await botClient.SendChatActionAsync(
            chatId, 
            ChatAction.Typing, 
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показати skeleton screen під час завантаження даних
    /// </summary>
    public static async Task<int> ShowLoadingSkeletonAsync(
        ITelegramBotClient botClient,
        long chatId,
        string title,
        int rowsCount = 3,
        CancellationToken cancellationToken = default)
    {
        await ShowTypingAsync(botClient, chatId, cancellationToken);

        var skeletonText = BuildSkeletonText(title, rowsCount);

        var message = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: skeletonText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        return message.MessageId;
    }

    /// <summary>
    /// Показати progress bar під час завантаження
    /// </summary>
    public static async Task<int> ShowProgressBarAsync(
        ITelegramBotClient botClient,
        long chatId,
        string title,
        int percentage = 0,
        CancellationToken cancellationToken = default)
    {
        await ShowTypingAsync(botClient, chatId, cancellationToken);

        var progressText = BuildProgressBarText(title, percentage);

        var message = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: progressText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        return message.MessageId;
    }

    /// <summary>
    /// Оновити progress bar
    /// </summary>
    public static async Task UpdateProgressBarAsync(
        ITelegramBotClient botClient,
        long chatId,
        int messageId,
        string title,
        int percentage,
        CancellationToken cancellationToken = default)
    {
        var progressText = BuildProgressBarText(title, percentage);

        try
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: progressText,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Ігноруємо помилки при оновленні (наприклад, якщо текст не змінився)
        }
    }

    /// <summary>
    /// Замінити skeleton screen на реальні дані
    /// </summary>
    public static async Task ReplaceSkeletonWithDataAsync(
        ITelegramBotClient botClient,
        long chatId,
        int skeletonMessageId,
        string actualContent,
        Telegram.Bot.Types.ReplyMarkups.IReplyMarkup? replyMarkup = null,
        CancellationToken cancellationToken = default)
    {
        await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: skeletonMessageId,
            text: actualContent,
            parseMode: ParseMode.Html,
            replyMarkup: replyMarkup as Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показати індикатор завантаження для конкретної дії
    /// </summary>
    public static async Task ShowActionLoadingAsync(
        ITelegramBotClient botClient,
        long chatId,
        string actionName,
        CancellationToken cancellationToken = default)
    {
        await ShowTypingAsync(botClient, chatId, cancellationToken);

        var loadingText = $"⏳ {actionName}...\n\n" +
                         "Це займе кілька секунд.";

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: loadingText,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Побудувати skeleton text
    /// </summary>
    private static string BuildSkeletonText(string title, int rowsCount)
    {
        var skeleton = $"<b>{title}</b>\n\n";
        skeleton += "⏳ Завантаження...\n\n";

        for (int i = 0; i < rowsCount; i++)
        {
            skeleton += "▭▭▭▭▭▭▭▭▭▭▭▭▭▭\n";
            skeleton += "▭▭▭▭▭▭▭▭▭▭\n\n";
        }

        return skeleton;
    }

    /// <summary>
    /// Побудувати progress bar text
    /// </summary>
    private static string BuildProgressBarText(string title, int percentage)
    {
        percentage = Math.Clamp(percentage, 0, 100);

        var filledBlocks = percentage / 10;
        var emptyBlocks = 10 - filledBlocks;

        var progressBar = new string('█', filledBlocks) + new string('░', emptyBlocks);

        return $"<b>{title}</b>\n\n" +
               $"Завантаження... [{progressBar}] {percentage}%";
    }

    /// <summary>
    /// Створити animated dots для loading (можна викликати кілька разів)
    /// </summary>
    public static string GetAnimatedDots(int iteration)
    {
        var dots = (iteration % 4) switch
        {
            0 => "   ",
            1 => ".  ",
            2 => ".. ",
            3 => "...",
            _ => "   "
        };

        return dots;
    }

    /// <summary>
    /// Показати multi-step loading process
    /// </summary>
    public static async Task<int> ShowMultiStepLoadingAsync(
        ITelegramBotClient botClient,
        long chatId,
        string title,
        List<string> steps,
        int currentStep = 0,
        CancellationToken cancellationToken = default)
    {
        await ShowTypingAsync(botClient, chatId, cancellationToken);

        var loadingText = $"<b>{title}</b>\n\n";

        for (int i = 0; i < steps.Count; i++)
        {
            if (i < currentStep)
            {
                loadingText += $"✅ {steps[i]}\n";
            }
            else if (i == currentStep)
            {
                loadingText += $"⏳ {steps[i]}...\n";
            }
            else
            {
                loadingText += $"⬜ {steps[i]}\n";
            }
        }

        var message = await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: loadingText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        return message.MessageId;
    }

    /// <summary>
    /// Оновити multi-step loading
    /// </summary>
    public static async Task UpdateMultiStepLoadingAsync(
        ITelegramBotClient botClient,
        long chatId,
        int messageId,
        string title,
        List<string> steps,
        int currentStep,
        CancellationToken cancellationToken = default)
    {
        var loadingText = $"<b>{title}</b>\n\n";

        for (int i = 0; i < steps.Count; i++)
        {
            if (i < currentStep)
            {
                loadingText += $"✅ {steps[i]}\n";
            }
            else if (i == currentStep)
            {
                loadingText += $"⏳ {steps[i]}...\n";
            }
            else
            {
                loadingText += $"⬜ {steps[i]}\n";
            }
        }

        try
        {
            await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: loadingText,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Ігноруємо помилки
        }
    }
}
