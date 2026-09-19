using System.ComponentModel.DataAnnotations;

namespace CompanyERP.Entities.Asset;

public enum AssetStatus
{
    [Display(Name = "Registered")]
    Registered,

    [Display(Name = "Active")]
    Active,

    [Display(Name = "In Maintenance")]
    InMaintenance,

    [Display(Name = "Disposed")]
    Disposed
}

public enum AssetDepreciationMethod
{
    [Display(Name = "Straight Line")]
    StraightLine,

    [Display(Name = "Reducing Balance")]
    ReducingBalance
}

public enum AssetMaintenanceType
{
    [Display(Name = "Preventive")]
    Preventive,

    [Display(Name = "Corrective")]
    Corrective
}

public enum DisposalResult
{
    [Display(Name = "No Gain/Loss")]
    NoGainLoss = 0,

    [Display(Name = "Gain")]
    Gain = 1,

    [Display(Name = "Loss")]
    Loss = 2
}

public enum AcquisitionPaymentType
{
    [Display(Name = "Cash")]
    Cash,

    [Display(Name = "Bank")]
    Bank,

    [Display(Name = "Payable")]
    Payable
}