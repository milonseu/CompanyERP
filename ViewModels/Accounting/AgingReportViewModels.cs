namespace CompanyERP.ViewModels.Accounting;

public enum AgingBucket
{
    Current,
    Days1To30,
    Days31To60,
    Days61To90,
    Days90Plus
}

public class AgingInvoiceRowViewModel
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SourceType { get; set; } = "Invoice";
    public DateTime? EffectiveDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int DaysOverdue { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
    public AgingBucket Bucket { get; set; }
}

public class AgingSummaryRowViewModel
{
    public int PartyId { get; set; }
    public string PartyCode { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Days90Plus { get; set; }
    public decimal TotalOutstanding => Current + Days1To30 + Days31To60 + Days61To90 + Days90Plus;
}

public class AgingReportViewModel
{
    public bool IsReceivables { get; set; } = true;
    public string Title { get; set; } = "Receivable Aging";
    public string PartyCaption { get; set; } = "Customer";
    public int CompanyId { get; set; }
    public DateTime AsOfDate { get; set; } = DateTime.Today;
    public int? BranchId { get; set; }
    public int? PartyId { get; set; }
    public List<AgingSummaryRowViewModel> Rows { get; set; } = new();
    public List<AgingInvoiceRowViewModel> Invoices { get; set; } = new();
    public decimal TotalCurrent { get; set; }
    public decimal TotalDays1To30 { get; set; }
    public decimal TotalDays31To60 { get; set; }
    public decimal TotalDays61To90 { get; set; }
    public decimal TotalDays90Plus { get; set; }
    public decimal TotalOutstanding =>
        TotalCurrent + TotalDays1To30 + TotalDays31To60 + TotalDays61To90 + TotalDays90Plus;
}