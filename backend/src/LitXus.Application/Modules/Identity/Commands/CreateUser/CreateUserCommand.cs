using LitXus.Application.Modules.Identity.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Identity.Commands.CreateUser;

public record CreateUserCommand(string Email, string FullName, string Password, Guid RoleId) : IRequest<UserSummaryDto>;
