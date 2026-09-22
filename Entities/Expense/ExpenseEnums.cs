using System.ComponentModel.DataAnnotations;

namespace CompanyERP.Entities.Expense;

public enum ExpensePaymentType
{
    [Display(Name = "Cash")]
    Cash,

    [Display(Name = "Bank")]
    Bank,

    [Display(Name = "Payable")]
    Payable
}