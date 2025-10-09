using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Admin.Queries.GetBackups;

public class GetBackupsQuery : IRequest<Result<List<BackupInfoDto>>>
{
    public long AdminId { get; set; }
}