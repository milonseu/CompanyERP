using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Asset;

public class AssetDisposal : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Disposal date is required.")]
    [Display(Name = "Disposal Date")]
    [DataType(DataType.Date)]
    public DateTime DisposalDate { get; set; } = DateTime.Today;

    [Range(0, double.MaxValue, ErrorMessage = "Sale value must be zero or more.")]
    [Display(Name = "Sale Value")]
    public decimal SaleValue { get; set; }

    [Display(Name = "Book Value at Disposal")]
    public decimal BookValueAtDisposal { get; set; }

    public DisposalResult Result { get; set; } = DisposalResult.NoGainLoss;

    [Display(Name = "Gain/Loss Amount")]
    public decimal GainLossAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
}