using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.Services;

/// <summary>
/// Сервіс автоматичного призначення апелів адміністраторам
/// </summary>
public class AppealAssignmentService : IAppealAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AppealAssignmentService> _logger;

    public AppealAssignmentService(
        IUnitOfWork unitOfWork,
        ILogger<AppealAssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BotUser>> AssignAppealAsync(Appeal appeal, CancellationToken cancellationToken = default)
    {
        try
        {
            // Знаходимо найкращого адміністратора для цього апела
            var bestAdminResult = await FindBestAdminForAppealAsync(appeal.Category, appeal.Priority, cancellationToken);
            if (!bestAdminResult.IsSuccess)
            {
                return Result<BotUser>.Fail(bestAdminResult.Error);
            }

            var bestAdmin = bestAdminResult.Value;

            // Призначаємо апел
            var assignResult = await AssignAppealToAdminAsync(appeal, bestAdmin.TelegramId, cancellationToken);
            if (!assignResult.IsSuccess)
            {
                return Result<BotUser>.Fail(assignResult.Error);
            }

            _logger.LogInformation(
                "Апел {AppealId} автоматично призначено адміністратору {AdminId} (навантаження: {CurrentLoad})",
                appeal.Id, bestAdmin.TelegramId, "TBD");

            return Result<BotUser>.Ok(bestAdmin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при автоматичному призначенні апела {AppealId}", appeal.Id);
            return Result<BotUser>.Fail("Не вдалося автоматично призначити апел");
        }
    }

    public async Task<Result> AssignAppealToAdminAsync(Appeal appeal, long adminId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Перевіряємо чи існує адміністратор
            var admin = await _unitOfWork.Users.GetByTelegramIdAsync(adminId, cancellationToken);
            if (admin == null || (admin.Role != UserRole.Admin && admin.Role != UserRole.SuperAdmin))
            {
                return Result.Fail("Адміністратор не знайдений або не має відповідних прав");
            }

            // Перевіряємо чи адміністратор доступний
            var adminWorkload = await _unitOfWork.AdminWorkloads
                .GetByAdminIdAsync(adminId, cancellationToken);

            if (adminWorkload != null && !adminWorkload.IsAvailable)
            {
                return Result.Fail("Адміністратор наразі недоступний");
            }

            // Призначаємо апел
            appeal.AssignTo(adminId);

            // Оновлюємо навантаження адміністратора
            if (adminWorkload != null)
            {
                adminWorkload.AssignAppeal();
            }
            else
            {
                // Створюємо новий запис навантаження
                adminWorkload = AdminWorkload.Create(adminId);
                adminWorkload.AssignAppeal();
                await _unitOfWork.AdminWorkloads.AddAsync(adminWorkload, cancellationToken);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при призначенні апела {AppealId} адміністратору {AdminId}", appeal.Id, adminId);
            return Result.Fail("Не вдалося призначити апел адміністратору");
        }
    }

    public async Task<Result<BotUser>> ReassignAppealAsync(Appeal appeal, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            // Якщо апел уже призначений, оновлюємо навантаження попереднього адміна
            if (appeal.AssignedToAdminId.HasValue)
            {
                await UpdateAdminWorkloadAsync(
                    appeal.AssignedToAdminId.Value,
                    AppealStatus.InProgress,
                    AppealStatus.New,
                    cancellationToken);
            }

            // Знаходимо нового адміністратора
            return await AssignAppealAsync(appeal, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перепризначенні апела {AppealId}", appeal.Id);
            return Result<BotUser>.Fail("Не вдалося перепризначити апел");
        }
    }

    public async Task<Result<BotUser>> FindBestAdminForAppealAsync(AppealCategory category, AppealPriority priority, CancellationToken cancellationToken = default)
    {
        try
        {
            // Отримуємо всіх доступних адміністраторів з їх експертизою
            var availableAdmins = await _unitOfWork.AdminWorkloads
                .GetAvailableAdminsOrderedByWorkloadAsync(cancellationToken);

            if (!availableAdmins.Any())
            {
                return Result<BotUser>.Fail("Немає доступних адміністраторів");
            }

            // Сортуємо адміністраторів за пріоритетом призначення
            var sortedAdmins = availableAdmins
                .Select(admin => new
                {
                    Admin = admin,
                    Score = CalculateAdminScore(admin, category, priority)
                })
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Admin.CalculateAssignmentPriority())
                .ToList();

            var bestAdmin = sortedAdmins.First().Admin;

            _logger.LogInformation(
                "Обрано найкращого адміністратора {AdminId} для категорії {Category} (скор: {Score})",
                bestAdmin.AdminId, category, sortedAdmins.First().Score);

            return Result<BotUser>.Ok(bestAdmin.Admin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при пошуку найкращого адміністратора для категорії {Category}", category);
            return Result<BotUser>.Fail("Не вдалося знайти підходящого адміністратора");
        }
    }

    public async Task<Result<IEnumerable<BotUser>>> GetAvailableAdminsForCategoryAsync(AppealCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            var availableAdmins = await _unitOfWork.AdminWorkloads
                .GetAdminsWithCategoryExpertiseAsync(category, cancellationToken);

            var adminUsers = availableAdmins.Select(w => w.Admin).ToList();
            return Result<IEnumerable<BotUser>>.Ok(adminUsers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні доступних адміністраторів для категорії {Category}", category);
            return Result<IEnumerable<BotUser>>.Fail("Не вдалося отримати список адміністраторів");
        }
    }

    public async Task<Result<int>> EscalateOverdueAppealsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Знаходимо прострочені апели (більше 24 годин без відповіді)
            var overdueThreshold = DateTime.UtcNow.AddHours(-24);
            
            var appealRepo = _unitOfWork.GetRepository<Appeal>();
            var overdueAppeals = await appealRepo
                .GetAllAsync(cancellationToken);

            // Фільтруємо прострочені апели в пам'яті (для простоти)
            var filteredAppeals = overdueAppeals
                .Where(a => a.Status == AppealStatus.New || 
                           (a.Status == AppealStatus.InProgress && 
                            a.FirstResponseAt == null &&
                            a.CreatedAt < overdueThreshold))
                .ToList();

            int escalatedCount = 0;

            foreach (var appeal in filteredAppeals)
            {
                // Спробуємо перепризначити апел
                var reassignResult = await ReassignAppealAsync(appeal, "Автоматична ескалація через просрочку", cancellationToken);
                if (reassignResult.IsSuccess)
                {
                    escalatedCount++;
                }
            }

            _logger.LogInformation("Ескальовано {Count} прострочених апелів", escalatedCount);
            return Result<int>.Ok(escalatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при ескалації прострочених апелів");
            return Result<int>.Fail("Не вдалося ескалювати прострочені апели");
        }
    }

    public async Task<Result> UpdateAdminWorkloadAsync(long adminId, AppealStatus oldStatus, AppealStatus newStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminWorkload = await _unitOfWork.AdminWorkloads
                .GetByAdminIdAsync(adminId, cancellationToken);

            if (adminWorkload == null)
            {
                // Створюємо новий запис якщо не існує
                adminWorkload = AdminWorkload.Create(adminId);
                await _unitOfWork.AdminWorkloads.AddAsync(adminWorkload, cancellationToken);
            }

            // Оновлюємо навантаження в залежності від зміни статусу
            if (IsActiveStatus(newStatus) && !IsActiveStatus(oldStatus))
            {
                adminWorkload.AssignAppeal();
            }
            else if (!IsActiveStatus(newStatus) && IsActiveStatus(oldStatus))
            {
                adminWorkload.CompleteAppeal();
            }

            adminWorkload.UpdateActivity();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при оновленні навантаження адміністратора {AdminId}", adminId);
            return Result.Fail("Не вдалося оновити навантаження адміністратора");
        }
    }

    public async Task<Result> SetAdminAvailabilityAsync(long adminId, bool isAvailable, CancellationToken cancellationToken = default)
    {
        try
        {
            var adminWorkload = await _unitOfWork.AdminWorkloads
                .GetByAdminIdAsync(adminId, cancellationToken);

            if (adminWorkload == null)
            {
                adminWorkload = AdminWorkload.Create(adminId);
                await _unitOfWork.AdminWorkloads.AddAsync(adminWorkload, cancellationToken);
            }

            adminWorkload.SetAvailability(isAvailable);

            _logger.LogInformation("Адміністратор {AdminId} змінив доступність на {IsAvailable}", adminId, isAvailable);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при зміні доступності адміністратора {AdminId}", adminId);
            return Result.Fail("Не вдалося змінити доступність");
        }
    }

    public async Task<Result<AdminWorkloadStats>> GetWorkloadStatsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var adminWorkloadsEnumerable = await _unitOfWork.AdminWorkloads
                .GetWorkloadStatsAsync(cancellationToken);
            var adminWorkloads = adminWorkloadsEnumerable.ToList();

            var stats = new AdminWorkloadStats
            {
                TotalAdmins = adminWorkloads.Count,
                AvailableAdmins = adminWorkloads.Count(w => w.IsAvailable),
                TotalActiveAppeals = adminWorkloads.Sum(w => w.ActiveAppealsCount),
                AverageAppealsPerAdmin = adminWorkloads.Count > 0 
                    ? (double)adminWorkloads.Sum(w => w.ActiveAppealsCount) / adminWorkloads.Count 
                    : 0,
                MostLoadedAdmin = adminWorkloads
                    .OrderByDescending(w => w.ActiveAppealsCount)
                    .Select(w => new AdminWorkloadInfo
                    {
                        AdminId = w.AdminId,
                        AdminName = w.Admin.FullName ?? w.Admin.FirstName ?? "Невідомий",
                        ActiveAppeals = w.ActiveAppealsCount,
                        TotalAppeals = w.TotalAppealsCount,
                        IsAvailable = w.IsAvailable,
                        LastActivityAt = w.LastActivityAt
                    })
                    .FirstOrDefault() ?? new AdminWorkloadInfo(),
                LeastLoadedAdmin = adminWorkloads
                    .Where(w => w.IsAvailable)
                    .OrderBy(w => w.ActiveAppealsCount)
                    .Select(w => new AdminWorkloadInfo
                    {
                        AdminId = w.AdminId,
                        AdminName = w.Admin.FullName ?? w.Admin.FirstName ?? "Невідомий",
                        ActiveAppeals = w.ActiveAppealsCount,
                        TotalAppeals = w.TotalAppealsCount,
                        IsAvailable = w.IsAvailable,
                        LastActivityAt = w.LastActivityAt
                    })
                    .FirstOrDefault() ?? new AdminWorkloadInfo()
            };

            return Result<AdminWorkloadStats>.Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні статистики навантаження");
            return Result<AdminWorkloadStats>.Fail("Не вдалося отримати статистику навантаження");
        }
    }

    private static int CalculateAdminScore(AdminWorkload adminWorkload, AppealCategory category, AppealPriority priority)
    {
        var baseScore = 100;

        // Експертиза в категорії
        var categoryExpertise = adminWorkload.CategoryExpertises
            .FirstOrDefault(e => e.Category == category);

        if (categoryExpertise != null)
        {
            baseScore += categoryExpertise.CalculateExpertiseScore();
        }

        // Штраф за навантаження
        baseScore -= adminWorkload.ActiveAppealsCount * 20;

        // Бонус за недавню активність
        var hoursSinceActivity = (DateTime.UtcNow - adminWorkload.LastActivityAt).TotalHours;
        if (hoursSinceActivity < 4)
        {
            baseScore += 30;
        }

        // Бонус за високий пріоритет для досвідчених адмінів
        if (priority == AppealPriority.High && categoryExpertise?.ExperienceLevel >= 4)
        {
            baseScore += 50;
        }

        return Math.Max(0, baseScore);
    }

    private static bool IsActiveStatus(AppealStatus status)
    {
        return status == AppealStatus.New || status == AppealStatus.InProgress;
    }
}