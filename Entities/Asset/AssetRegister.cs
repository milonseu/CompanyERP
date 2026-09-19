using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.Company;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Asset;

public class AssetRegister : BaseEntity
{
    [Required(ErrorMessage = "Company is required.")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch is required.")]
    public int BranchId { get; set; }

    [Required(ErrorMessage = "Asset type is required.")]
    public int AssetTypeId { get; set; }

    [Required(ErrorMessage = "Asset number is required.")]
    [StringLength(30)]
    [Display(Name = "Asset No")]
    public string AssetNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Asset name is required.")]
    [StringLength(150)]
    [Display(Name = "Asset Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNo { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [Required(ErrorMessage = "Purchase date is required.")]
    [Display(Name = "Purchase Date")]
    [DataType(DataType.Date)]
    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    [Range(0, double.MaxValue, ErrorMessage = "Cost must be zero or more.")]
    public decimal Cost { get; set; }

    [Range(1, 600, ErrorMessage = "Useful life must be between 1 and 600 months.")]
    [Display(Name = "Useful Life (Months)")]
    public int UsefulLifeMonths { get; set; } = 36;

    [Range(0, double.MaxValue, ErrorMessage = "Salvage value must be zero or more.")]
    [Display(Name = "Salvage Value")]
    public decimal SalvageValue { get; set; }

    [Display(Name = "Depreciation Method")]
    public AssetDepreciationMethod DepreciationMethod { get; set; } = AssetDepreciationMethod.StraightLine;

    public decimal AccumulatedDepreciation { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Registered;

    [StringLength(300)]
    public string? Location { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public CompanyProfile? Company { get; set; }
    public Branch? Branch { get; set; }
    public AssetType? AssetType { get; set; }
    public AssetAcquisition? Acquisition { get; set; }
    public ICollection<AssetAssignment> Assignments { get; set; } = [];
    public ICollection<AssetTransfer> Transfers { get; set; } = [];
    public ICollection<AssetMaintenance> Maintenances { get; set; } = [];
    public ICollection<AssetDepreciation> Depreciations { get; set; } = [];
    public AssetDisposal? Disposal { get; set; }
    public ICollection<AssetDocument> Documents { get; set; } = [];

    [NotMapped]
    public decimal BookValue => Cost - AccumulatedDepreciation;

    [NotMapped]
    public decimal DepreciableAmount => Math.Max(0, Cost - SalvageValue);
}