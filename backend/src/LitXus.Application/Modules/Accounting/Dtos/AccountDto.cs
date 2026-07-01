using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Modules.Accounting.Dtos;

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    Guid? ParentAccountId,
    bool IsActive,
    decimal Balance);

public static class AccountMappingExtensions
{
    public static AccountDto ToDto(this Account account) => new(
        account.Id,
        account.Code,
        account.Name,
        account.Type.ToString(),
        account.ParentAccountId,
        account.IsActive,
        account.Balance);
}
