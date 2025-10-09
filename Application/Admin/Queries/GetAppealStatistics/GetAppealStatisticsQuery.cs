using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Admin.Queries.GetAppealStatistics;

public class GetAppealStatisticsQuery : IRequest<Result<AppealStatisticsDto>>
{
    public long AdminId { get; set; }
    public int? Days { get; set; } = 30; // За скільки днів показувати статистику
}

public class AppealStatisticsDto
{
    public int TotalAppeals { get; set; }
    public int OpenAppeals { get; set; }
    public int ClosedAppeals { get; set; }
    public int InProgressAppeals { get; set; }
    
    public double AverageResolutionTimeHours { get; set; }
    public string FormattedAverageResolutionTime => FormatResolutionTime(AverageResolutionTimeHours);
    
    public List<CategoryStatDto> CategoryBreakdown { get; set; } = new();
    public List<PriorityStatDto> PriorityBreakdown { get; set; } = new();
    public List<DailyStatDto> DailyStats { get; set; } = new();
    
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    private static string FormatResolutionTime(double hours)
    {
        if (hours < 1)
        {
            return $"{hours * 60:0} хв";
        }
        if (hours < 24)
        {
            return $"{hours:0.1} год";
        }
        var days = (int)(hours / 24);
        var remainingHours = hours % 24;
        return remainingHours > 0 ? $"{days}д {remainingHours:0}г" : $"{days} днів";
    }
}

public class CategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Icon => Category switch
    {
        "Академічна" => "📚",
        "Соціальна" => "🤝",
        "Фінансова" => "💰",
        "Інша" => "❓",
        _ => "📋"
    };
}

public class PriorityStatDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Icon => Priority switch
    {
        "Низький" => "🟢",
        "Середній" => "🟡",
        "Високий" => "🔴",
        _ => "⚪"
    };
}

public class DailyStatDto
{
    public DateTime Date { get; set; }
    public int Created { get; set; }
    public int Closed { get; set; }
    public string FormattedDate => Date.ToString("dd.MM");
}