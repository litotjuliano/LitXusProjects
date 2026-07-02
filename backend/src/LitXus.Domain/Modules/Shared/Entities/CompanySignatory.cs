using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Shared.Entities;

/// <summary>Director, finance manager, or other authorized signatory for the single Company row.</summary>
public class CompanySignatory : BaseEntity, IAuditable
{
    public Guid CompanyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string IcNumber { get; private set; } = string.Empty;
    public string Position { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }

    private CompanySignatory() { }

    public static CompanySignatory Create(Guid companyId, string name, string icNumber, string position, string email, string? phone)
    {
        return new CompanySignatory
        {
            CompanyId = companyId,
            Name = name,
            IcNumber = icNumber,
            Position = position,
            Email = email,
            Phone = phone,
        };
    }
}
