using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Sales.Commands.RejectPayment;
using LitXus.Application.Modules.Sales.Commands.VerifyPayment;
using LitXus.Application.Modules.Sales.Queries.GetPayments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record RejectPaymentRequest(string Reason);

[ApiController]
[Authorize]
[Route("api/v1/sales/payments")]
[RequireModule(Module.Sales)]
public class PaymentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("Sales.Payment.Read")]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var result = await mediator.Send(new GetPaymentsQuery(status));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/verify")]
    [RequirePermission("Sales.Payment.Verify")]
    public async Task<IActionResult> Verify(Guid id)
    {
        var result = await mediator.Send(new VerifyPaymentCommand(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission("Sales.Payment.Verify")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPaymentRequest request)
    {
        var result = await mediator.Send(new RejectPaymentCommand(id, request.Reason));
        return Ok(new { data = result, meta = (object?)null });
    }
}
