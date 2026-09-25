using System.ComponentModel.DataAnnotations;

namespace CompanyERP.Entities.Payment;

public enum PaymentCategory
{
    [Display(Name = "Customer")]
    Customer,

    [Display(Name = "Supplier")]
    Supplier,

    [Display(Name = "Expense")]
    Expense,

    [Display(Name = "Salary")]
    Salary,

    [Display(Name = "Asset")]
    Asset
}

public enum PaymentAccountType
{
    [Display(Name = "Cash")]
    Cash,

    [Display(Name = "Bank")]
    Bank
}

public enum PaymentStatus
{
    [Display(Name = "Posted")]
    Posted,

    [Display(Name = "Void")]
    Void
}