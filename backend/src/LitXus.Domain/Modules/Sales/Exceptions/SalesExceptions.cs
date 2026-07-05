using LitXus.Domain.Common;

namespace LitXus.Domain.Modules.Sales.Exceptions;

public sealed class InvoiceNotDraftException()
    : DomainException("INVOICE_NOT_DRAFT", "Only Draft invoices can be edited or issued.");

public sealed class InvoiceTooFewLinesException()
    : DomainException("INVOICE_TOO_FEW_LINES", "An invoice needs at least 1 line to be issued.");

public sealed class InvoiceHasVerifiedPaymentException()
    : DomainException("INVOICE_HAS_VERIFIED_PAYMENT", "This invoice has a verified payment and cannot be voided.");

public sealed class InvoiceNotVoidableException()
    : DomainException("INVOICE_NOT_VOIDABLE", "Only Issued, PartiallyPaid, or Paid invoices can be voided.");

public sealed class InvoiceVoidRequiresReasonException()
    : DomainException("INVOICE_VOID_REQUIRES_REASON", "A reason is required to void an invoice.");

public sealed class PaymentExceedsOutstandingBalanceException(decimal outstanding)
    : DomainException("PAYMENT_EXCEEDS_OUTSTANDING_BALANCE", $"Payment amount exceeds the invoice's outstanding balance of RM {outstanding:N2}.");

public sealed class PaymentNotPendingException()
    : DomainException("PAYMENT_NOT_PENDING", "Only Pending payments can be verified or rejected.");

public sealed class RejectRequiresReasonException()
    : DomainException("REJECT_REQUIRES_REASON", "A reason is required to reject a payment.");

public sealed class CreditNoteExceedsInvoiceBalanceException(decimal outstanding)
    : DomainException("CREDIT_NOTE_EXCEEDS_INVOICE_BALANCE", $"Credit note amount exceeds the invoice's outstanding balance of RM {outstanding:N2}.");

public sealed class CustomerCodeDuplicateException(string code)
    : DomainException("CUSTOMER_CODE_DUPLICATE", $"A customer with code '{code}' already exists.");

public sealed class CustomerInactiveException(string code)
    : DomainException("CUSTOMER_INACTIVE", $"Customer '{code}' is inactive and cannot be invoiced.");

public sealed class SalesSettingsNotConfiguredException()
    : DomainException("SALES_SETTINGS_NOT_CONFIGURED", "Sales GL account mapping hasn't been configured yet. Ask an Admin to set it up in Sales Settings.");
