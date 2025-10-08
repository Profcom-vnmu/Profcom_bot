using MediatR;
using StudentUnionBot.Application.Contacts.DTOs;
using StudentUnionBot.Core.Results;

namespace StudentUnionBot.Application.Contacts.Queries.GetAllContacts;

/// <summary>
/// Query для отримання всіх активних контактів
/// </summary>
public class GetAllContactsQuery : IRequest<Result<ContactListDto>>
{
}
