using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Queries.GetUsers;

public record GetUsersQuery : IRequest<IReadOnlyList<UserSummaryDto>>;
