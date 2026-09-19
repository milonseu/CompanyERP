using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.Entities.Asset;

public class AssetTransfer : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Display(Name = "From Branch")]
    public int? FromBranchId { get; set; }

    [Required(ErrorMessage = "To branch is required.")]
    [Display(Name = "To Branch")]
    public int ToBranchId { get; set; }

    [Required(ErrorMessage = "Transfer date is required.")]
    [Display(Name = "Transfer Date")]
    [DataType(DataType.Date)]
    public DateTime TransferDate { get; set; } = DateTime.Today;

    [StringLength(300)]
    public string? FromLocation { get; set; }

    [StringLength(300)]
    public string? ToLocation { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
    public Branch? FromBranch { get; set; }
    public Branch? ToBranch { get; set; }
}