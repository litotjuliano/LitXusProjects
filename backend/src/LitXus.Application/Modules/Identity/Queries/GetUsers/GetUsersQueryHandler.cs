using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Queries.GetUsers;

public class GetUsersQueryHandler(IIdentityUserService identityUserService)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserSummaryDto>>
{
    public Task<IReadOnlyList<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken) =>
        identityUserService.GetUsersAsync(cancellationToken);
}
