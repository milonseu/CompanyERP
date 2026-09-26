namespace CompanyERP.ViewModels.Accounting;

public class StatementLineViewModel
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Type { get; set; } = "Invoice";
    public string Description { get; set; } = string.Empty;
    public decimal Invoice { get; set; }
    public decimal Payment { get; set; }
    public decimal Balance { get; set; }
}

public class StatementViewModel
{
    public bool IsCustomer { get; set; } = true;
    public string Title { get; set; } = "Customer Statement";
    public string PartyCaption { get; set; } = "Customer";
    public int CompanyId { get; set; }
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<StatementLineViewModel> Lines { get; set; } = new();
    public decimal TotalInvoice { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal ClosingBalance { get; set; }
}