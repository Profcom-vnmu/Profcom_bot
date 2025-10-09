using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Admin.Queries.GetAppealStatistics;

public class GetAppealStatisticsQueryHandler : IRequestHandler<GetAppealStatisticsQuery, Result<AppealStatisticsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAppealStatisticsQueryHandler> _logger;

    public GetAppealStatisticsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAppealStatisticsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AppealStatisticsDto>> Handle(GetAppealStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо, чи користувач є адміністратором
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (user == null)
            {
                return Result<AppealStatisticsDto>.Fail("Користувача не знайдено");
            }

            if (user.Role < UserRole.Admin)
            {
                return Result<AppealStatisticsDto>.Fail("Недостатньо прав для перегляду статистики");
            }

            var days = request.Days ?? 30;
            var fromDate = DateTime.UtcNow.Date.AddDays(-days);
            var toDate = DateTime.UtcNow.Date.AddDays(1);

            // Отримуємо всі звернення за період
            var appeals = await _unitOfWork.Appeals.GetAppealsByDateRangeAsync(fromDate, toDate, cancellationToken);

            var statistics = new AppealStatisticsDto
            {
                FromDate = fromDate,
                ToDate = DateTime.UtcNow.Date,
                TotalAppeals = appeals.Count
            };

            // Підрахунок за статусами
            statistics.OpenAppeals = appeals.Count(a => a.Status == AppealStatus.New);
            statistics.InProgressAppeals = appeals.Count(a => a.Status == AppealStatus.InProgress);
            statistics.ClosedAppeals = appeals.Count(a => a.Status == AppealStatus.Closed);

            // Середній час розгляду для закритих звернень
            var closedAppealsWithDate = appeals.Where(a => a.Status == AppealStatus.Closed && a.ClosedAt.HasValue).ToList();
            if (closedAppealsWithDate.Any())
            {
                var totalResolutionHours = closedAppealsWithDate
                    .Select(a => (a.ClosedAt!.Value - a.CreatedAt).TotalHours)
                    .Sum();
                statistics.AverageResolutionTimeHours = totalResolutionHours / closedAppealsWithDate.Count;
            }

            // Статистика по категоріях
            var categoryGroups = appeals
                .GroupBy(a => a.Category.GetDisplayName())
                .Select(g => new CategoryStatDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = appeals.Count > 0 ? (double)g.Count() / appeals.Count * 100 : 0
                })
                .OrderByDescending(c => c.Count)
                .ToList();
            statistics.CategoryBreakdown = categoryGroups;

            // Статистика по пріоритетах
            var priorityGroups = appeals
                .GroupBy(a => a.Priority.GetDisplayName())
                .Select(g => new PriorityStatDto
                {
                    Priority = g.Key,
                    Count = g.Count(),
                    Percentage = appeals.Count > 0 ? (double)g.Count() / appeals.Count * 100 : 0
                })
                .OrderByDescending(p => p.Count)
                .ToList();
            statistics.PriorityBreakdown = priorityGroups;

            // Денна статистика за останні 14 днів
            var dailyStats = new List<DailyStatDto>();
            for (int i = 13; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var dayAppeals = appeals.Where(a => a.CreatedAt.Date == date).ToList();
                var dayClosed = appeals.Where(a => a.ClosedAt?.Date == date && a.Status == AppealStatus.Closed).ToList();

                dailyStats.Add(new DailyStatDto
                {
                    Date = date,
                    Created = dayAppeals.Count,
                    Closed = dayClosed.Count
                });
            }
            statistics.DailyStats = dailyStats;

            _logger.LogInformation(
                "Згенеровано статистику звернень для адміністратора {AdminId}: {TotalAppeals} звернень за {Days} днів",
                request.AdminId,
                statistics.TotalAppeals,
                days);

            return Result<AppealStatisticsDto>.Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при генерації статистики звернень для адміністратора {AdminId}", request.AdminId);
            return Result<AppealStatisticsDto>.Fail("Виникла помилка при генерації статистики");
        }
    }
}