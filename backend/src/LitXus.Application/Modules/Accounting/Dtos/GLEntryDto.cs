using LitXus.Domain.Modules.Accounting.Entities;

namespace LitXus.Application.Modules.Accounting.Dtos;

public record GLEntryLineDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? LineDescription);

public record GLEntryDto(
    Guid Id,
    string? EntryNumber,
    DateOnly EntryDate,
    string Description,
    string Status,
    DateTime? PostedAtUtc,
    string? VoidReason,
    IReadOnlyList<GLEntryLineDto> Lines);

public static class GLEntryMappingExtensions
{
    public static GLEntryDto ToDto(this GLEntry entry) => new(
        entry.Id,
        entry.EntryNumber,
        entry.EntryDate,
        entry.Description,
        entry.Status.ToString(),
        entry.PostedAtUtc,
        entry.VoidReason,
        entry.Lines.Select(l => l.ToDto()).ToList());

    public static GLEntryLineDto ToDto(this GLEntryLine line) => new(
        line.Id,
        line.AccountId,
        line.Account.Code,
        line.Account.Name,
        line.DebitAmount,
        line.CreditAmount,
        line.LineDescription);
}
