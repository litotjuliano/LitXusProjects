using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetGeneralLedger;

public record GetGeneralLedgerQuery(Guid AccountId, DateOnly From, DateOnly To) : IRequest<GeneralLedgerDto>;
