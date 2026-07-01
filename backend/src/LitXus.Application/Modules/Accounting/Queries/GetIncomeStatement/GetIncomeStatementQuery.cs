using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetIncomeStatement;

public record GetIncomeStatementQuery(DateOnly From, DateOnly To) : IRequest<IncomeStatementDto>;
