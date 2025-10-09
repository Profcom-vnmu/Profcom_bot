using MediatR;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Admin.Commands.RestoreBackup;

public class RestoreBackupCommand : IRequest<Result<bool>>
{
    public long AdminId { get; set; }
    public string BackupFilePath { get; set; } = string.Empty;
}