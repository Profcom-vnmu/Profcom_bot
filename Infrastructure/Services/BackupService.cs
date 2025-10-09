using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Admin.Commands.CreateBackup;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using System.IO.Compression;

namespace StudentUnionBot.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;
    private readonly string _databasePath;
    private readonly string _backupDirectory;

    public BackupService(ILogger<BackupService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _databasePath = configuration.GetConnectionString("DefaultConnection")?.Replace("Data Source=", "") ?? "Data/studentunion.db";
        _backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        
        // Створюємо директорію для резервних копій
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public Task<Result<BackupResultDto>> CreateBackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Перевіряємо, чи існує база даних
            if (!File.Exists(_databasePath))
            {
                return Task.FromResult(Result<BackupResultDto>.Fail("Файл бази даних не знайдено"));
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"backup_{timestamp}.db";
            var backupFilePath = Path.Combine(_backupDirectory, backupFileName);

            // Копіюємо файл бази даних
            File.Copy(_databasePath, backupFilePath, true);
            
            // Отримуємо інформацію про файл
            var backupFileInfo = new FileInfo(backupFilePath);
            var originalFileInfo = new FileInfo(_databasePath);

            var result = new BackupResultDto
            {
                BackupFileName = backupFileName,
                BackupFilePath = backupFilePath,
                FileSizeBytes = backupFileInfo.Length,
                CreatedAt = DateTime.Now,
                DatabaseSize = FormatFileSize(originalFileInfo.Length)
            };

            _logger.LogInformation(
                "Створено резервну копію БД: {FileName}, розмір: {Size} bytes",
                backupFileName,
                backupFileInfo.Length);

            return Task.FromResult(Result<BackupResultDto>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при створенні резервної копії БД");
            return Task.FromResult(Result<BackupResultDto>.Fail($"Помилка при створенні резервної копії: {ex.Message}"));
        }
    }

    public Task<Result<bool>> RestoreBackupAsync(string backupFilePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // Перевіряємо, чи існує файл резервної копії
            if (!File.Exists(backupFilePath))
            {
                return Task.FromResult(Result<bool>.Fail("Файл резервної копії не знайдено"));
            }

            // Створюємо резервну копію поточної БД перед відновленням
            var currentBackupPath = $"{_databasePath}.backup_{DateTime.Now:yyyyMMdd_HHmmss}";
            if (File.Exists(_databasePath))
            {
                File.Copy(_databasePath, currentBackupPath, true);
            }

            // Відновлюємо базу даних
            File.Copy(backupFilePath, _databasePath, true);

            _logger.LogInformation(
                "Відновлено БД з резервної копії: {BackupPath}. Попередня БД збережена як: {CurrentBackupPath}",
                backupFilePath,
                currentBackupPath);

            return Task.FromResult(Result<bool>.Ok(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відновленні БД з резервної копії {BackupPath}", backupFilePath);
            return Task.FromResult(Result<bool>.Fail($"Помилка при відновленні: {ex.Message}"));
        }
    }

    public Task<Result<List<BackupInfoDto>>> GetAvailableBackupsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var backupFiles = Directory.GetFiles(_backupDirectory, "backup_*.db")
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .Take(20) // Показуємо тільки останні 20 копій
                .ToList();

            var backups = new List<BackupInfoDto>();

            foreach (var filePath in backupFiles)
            {
                var fileInfo = new FileInfo(filePath);
                
                backups.Add(new BackupInfoDto
                {
                    FileName = fileInfo.Name,
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    CreatedAt = fileInfo.CreationTime
                });
            }

            return Task.FromResult(Result<List<BackupInfoDto>>.Ok(backups));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні списку резервних копій");
            return Task.FromResult(Result<List<BackupInfoDto>>.Fail($"Помилка при отриманні списку: {ex.Message}"));
        }
    }

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