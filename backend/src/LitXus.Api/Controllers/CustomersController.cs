using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Commands.CreateCustomer;
using LitXus.Application.Modules.Sales.Commands.SetCustomerActive;
using LitXus.Application.Modules.Sales.Commands.UpdateCustomer;
using LitXus.Application.Modules.Sales.Queries.GetCustomers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record UpdateCustomerRequest(
    string CompanyName, string? ContactPerson, string? Email, string? Phone,
    string? Address, decimal CreditLimit, int PaymentTermsDays);

public record SetCustomerActiveRequest(bool IsActive);

[ApiController]
[Authorize]
[Route("api/v1/sales/customers")]
[RequireModule(Module.Sales)]
public class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Sales.Customer.Read")]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await mediator.Send(new GetCustomersQuery(includeInactive));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Sales.Customer.Create")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { data = result, meta = (object?)null });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Sales.Customer.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var result = await mediator.Send(new UpdateCustomerCommand(
            id, request.CompanyName, request.ContactPerson, request.Email, request.Phone,
            request.Address, request.CreditLimit, request.PaymentTermsDays));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPatch("{id:guid}/status")]
    [RequirePermission("Sales.Customer.Update")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetCustomerActiveRequest request)
    {
        await mediator.Send(new SetCustomerActiveCommand(id, request.IsActive));
        return NoContent();
    }
}
