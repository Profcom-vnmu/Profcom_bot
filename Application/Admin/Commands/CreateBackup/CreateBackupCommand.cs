using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Admin.Commands.CreateBackup;

public class CreateBackupCommand : IRequest<Result<BackupResultDto>>
{
    public long AdminId { get; set; }
}

public class BackupResultDto
{
    public string BackupFileName { get; set; } = string.Empty;
    public string BackupFilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DatabaseSize { get; set; } = string.Empty;
}