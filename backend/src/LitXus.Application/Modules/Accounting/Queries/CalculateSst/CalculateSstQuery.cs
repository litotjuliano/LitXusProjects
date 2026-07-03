using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.CalculateSst;

public record CalculateSstQuery(decimal SubTotal, Guid TaxCodeId) : IRequest<SstCalculationDto>;
