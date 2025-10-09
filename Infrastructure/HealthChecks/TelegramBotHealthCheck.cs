using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace StudentUnionBot.Infrastructure.HealthChecks;

/// <summary>
/// Health check для перевірки підключення до Telegram Bot API
/// </summary>
public class TelegramBotHealthCheck : IHealthCheck
{
    private readonly ITelegramBotClient _botClient;

    public TelegramBotHealthCheck(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Отримуємо інформацію про бота
            var me = await _botClient.GetMeAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "bot_username", me.Username ?? "unknown" },
                { "bot_id", me.Id },
                { "can_join_groups", me.CanJoinGroups },
                { "can_read_all_group_messages", me.CanReadAllGroupMessages },
                { "supports_inline_queries", me.SupportsInlineQueries }
            };

            return HealthCheckResult.Healthy(
                $"Telegram Bot API is healthy (bot: @{me.Username})",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Telegram Bot API connection failed",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}
