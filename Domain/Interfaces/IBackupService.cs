using StudentUnionBot.Application.Admin.Commands.CreateBackup;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Domain.Interfaces;

public interface IBackupService
{
    /// <summary>
    /// Створює резервну копію бази даних
    /// </summary>
    Task<Result<BackupResultDto>> CreateBackupAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Відновлює базу даних з резервної копії
    /// </summary>
    Task<Result<bool>> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримує список доступних резервних копій
    /// </summary>
    Task<Result<List<BackupInfoDto>>> GetAvailableBackupsAsync(CancellationToken cancellationToken = default);
}

public class BackupInfoDto
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedSize => FormatFileSize(FileSizeBytes);
    public string FormattedDate => CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}