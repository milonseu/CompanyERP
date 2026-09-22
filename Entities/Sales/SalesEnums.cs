using System.ComponentModel.DataAnnotations;

namespace CompanyERP.Entities.Sales;

public enum SalesItemType
{
    [Display(Name = "Product")]
    Product,

    [Display(Name = "Software")]
    Software,

    [Display(Name = "Service")]
    Service
}

public enum SalesOrderStatus
{
    Draft,
    Confirmed,
    Invoiced,
    Cancelled
}

public enum SalesInvoiceStatus
{
    Unpaid,
    PartiallyPaid,
    Paid
}

public enum SalesPaymentType
{
    [Display(Name = "Cash")]
    Cash,

    [Display(Name = "Bank")]
    Bank,

    [Display(Name = "On Account")]
    OnAccount
}

public enum ServiceKind
{
    [Display(Name = "Software")]
    Software,

    [Display(Name = "Service")]
    Service
}

public enum ServiceOrderStatus
{
    Pending,
    InProgress,
    Delivered,
    Cancelled
}

public enum SalesReturnStatus
{
    Draft,
    Posted
}