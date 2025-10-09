using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Files.DTOs;

/// <summary>
/// DTO для прикріпленого файла
/// </summary>
public class FileAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedFileSize { get; set; } = string.Empty;
    public FileType FileType { get; set; }
    public string FileTypeIcon { get; set; } = string.Empty;
    public string FileTypeName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
    public string? ThumbnailPath { get; set; }
    public bool HasThumbnail { get; set; }
    public long UploadedByUserId { get; set; }
    public string UploadedByUserName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public ScanStatus ScanStatus { get; set; }
    public string ScanStatusIcon { get; set; } = string.Empty;
    public string ScanStatusName { get; set; } = string.Empty;
    public string? ScanResult { get; set; }
    public DateTime? ScannedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsSafeToDownload { get; set; }
    public string? DownloadUrl { get; set; }
}

/// <summary>
/// DTO для зв'язку файла з апелом
/// </summary>
public class AppealFileAttachmentDto
{
    public int Id { get; set; }
    public int AppealId { get; set; }
    public FileAttachmentDto File { get; set; } = null!;
    public DateTime AttachedAt { get; set; }
    public long AttachedByUserId { get; set; }
    public string AttachedByUserName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEvidence { get; set; }
}

/// <summary>
/// DTO для списку файлів з пагінацією
/// </summary>
public class FileListDto
{
    public List<FileAttachmentDto> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

/// <summary>
/// DTO для статистики файлів
/// </summary>
public class FileUsageStatsDto
{
    public int TotalFiles { get; set; }
    public string TotalSize { get; set; } = string.Empty;
    public long TotalSizeBytes { get; set; }
    public int ActiveFiles { get; set; }
    public int DeletedFiles { get; set; }
    public List<FileTypeStatsDto> FilesByType { get; set; } = new();
    public List<ScanStatusStatsDto> FilesByScanStatus { get; set; } = new();
    public int FilesUploadedToday { get; set; }
    public int FilesUploadedThisWeek { get; set; }
    public int FilesUploadedThisMonth { get; set; }
    public string AverageFileSize { get; set; } = string.Empty;
    public FileAttachmentDto? LargestFile { get; set; }
    public int CompressedFiles { get; set; }
    public int FilesWithThumbnails { get; set; }
}

/// <summary>
/// DTO для статистики по типу файла
/// </summary>
public class FileTypeStatsDto
{
    public FileType Type { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string TypeIcon { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// DTO для статистики по статусу сканування
/// </summary>
public class ScanStatusStatsDto
{
    public ScanStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// DTO для результату завантаження файла
/// </summary>
public class FileUploadResultDto
{
    public FileAttachmentDto File { get; set; } = null!;
    public AppealFileAttachmentDto? AppealAttachment { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool RequiresApproval { get; set; }
    public string? ApprovalReason { get; set; }
}