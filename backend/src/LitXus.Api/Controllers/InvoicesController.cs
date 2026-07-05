using LitXus.Api.Filters;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Company.Queries.GetCompanyProfile;
using LitXus.Application.Modules.Sales.Commands.CreateInvoice;
using LitXus.Application.Modules.Sales.Commands.IssueInvoice;
using LitXus.Application.Modules.Sales.Commands.RecordPayment;
using LitXus.Application.Modules.Sales.Commands.UpdateInvoice;
using LitXus.Application.Modules.Sales.Commands.VoidInvoice;
using LitXus.Application.Modules.Sales.Queries.GetInvoiceById;
using LitXus.Application.Modules.Sales.Queries.GetInvoices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LitXus.Api.Controllers;

public record VoidInvoiceRequest(string Reason);

public record RecordPaymentRequest(DateOnly PaymentDate, decimal Amount, string Method, string? ReferenceNumber, Guid? BankAccountId);

[ApiController]
[Authorize]
[Route("api/v1/sales/invoices")]
[RequireModule(Module.Sales)]
public class InvoicesController(IMediator mediator, IInvoicePdfExporter pdfExporter) : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    [HttpGet]
    [RequirePermission("Sales.Invoice.Read")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? customerId,
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
    {
        var result = await mediator.Send(new GetInvoicesQuery(status, customerId, dateFrom, dateTo));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Sales.Invoice.Read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await mediator.Send(new GetInvoiceByIdQuery(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost]
    [RequirePermission("Sales.Invoice.Create")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Invoice.Id },
            new { data = result.Invoice, meta = new { creditLimitWarning = result.CreditLimitWarning } });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Sales.Invoice.Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateInvoiceCommand command)
    {
        var result = await mediator.Send(new UpdateInvoiceCommand(id, command.InvoiceDate, command.DueDate, command.Notes, command.Lines));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/issue")]
    [RequirePermission("Sales.Invoice.Approve")]
    public async Task<IActionResult> Issue(Guid id)
    {
        var result = await mediator.Send(new IssueInvoiceCommand(id));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpPost("{id:guid}/void")]
    [RequirePermission("Sales.Invoice.Approve")]
    public async Task<IActionResult> Void(Guid id, [FromBody] VoidInvoiceRequest request)
    {
        var result = await mediator.Send(new VoidInvoiceCommand(id, request.Reason));
        return Ok(new { data = result, meta = (object?)null });
    }

    [HttpGet("{id:guid}/pdf")]
    [RequirePermission("Sales.Invoice.Read")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var invoice = await mediator.Send(new GetInvoiceByIdQuery(id));
        var company = await mediator.Send(new GetCompanyProfileQuery());
        return File(pdfExporter.ExportInvoice(invoice, company), PdfContentType, $"invoice-{invoice.InvoiceNumber ?? id.ToString()}.pdf");
    }

    [HttpPost("{id:guid}/payments")]
    [RequirePermission("Sales.Payment.Create")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request)
    {
        var result = await mediator.Send(new RecordPaymentCommand(
            id, request.PaymentDate, request.Amount, request.Method, request.ReferenceNumber, request.BankAccountId));
        return StatusCode(StatusCodes.Status201Created, new { data = result, meta = (object?)null });
    }
}
