using LitXus.Application.Modules.Accounting.Dtos;
using MediatR;

namespace LitXus.Application.Modules.Accounting.Queries.GetReconciliationStatus;

public record GetReconciliationStatusQuery(Guid BankAccountId) : IRequest<ReconciliationStatusDto>;
