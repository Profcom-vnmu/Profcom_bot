using MediatR;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Admin.Commands.RestoreBackup;

[RequirePermission(Permission.RestoreBackup)]
public class RestoreBackupCommand : IRequest<Result<bool>>
{
    public long AdminId { get; set; }
    public string BackupFilePath { get; set; } = string.Empty;
}