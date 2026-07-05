using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Entities;

public class Customer : BaseEntity, IAuditable
{
    public string Code { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;

    private Customer() { }

    public static Customer Create(
        string code, string companyName, string? contactPerson, string? email, string? phone,
        string? address, decimal creditLimit, int paymentTermsDays)
    {
        return new Customer
        {
            Code = code,
            CompanyName = companyName,
            ContactPerson = contactPerson,
            Email = email,
            Phone = phone,
            Address = address,
            CreditLimit = creditLimit,
            PaymentTermsDays = paymentTermsDays,
        };
    }

    /// <summary>Code is immutable once set, matching Account.Rename's convention.</summary>
    public void Update(
        string companyName, string? contactPerson, string? email, string? phone,
        string? address, decimal creditLimit, int paymentTermsDays)
    {
        CompanyName = companyName;
        ContactPerson = contactPerson;
        Email = email;
        Phone = phone;
        Address = address;
        CreditLimit = creditLimit;
        PaymentTermsDays = paymentTermsDays;
    }

    public void SetActive(bool isActive) => IsActive = isActive;
}
