namespace CompanyERP.Entities.Purchase;

public enum PurchaseRequestStatus
{
    Draft,
    Approved,
    Converted,
    Cancelled
}

public enum PurchaseQuotationStatus
{
    Draft,
    Sent,
    Accepted,
    Cancelled
}

public enum PurchaseOrderStatus
{
    Draft,
    Approved,
    Received,
    Cancelled
}

public enum PurchaseInvoiceStatus
{
    Unpaid,
    PartiallyPaid,
    Paid
}