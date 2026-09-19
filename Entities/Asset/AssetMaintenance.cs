using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Asset;

public class AssetMaintenance : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Maintenance date is required.")]
    [Display(Name = "Maintenance Date")]
    [DataType(DataType.Date)]
    public DateTime MaintenanceDate { get; set; } = DateTime.Today;

    public AssetMaintenanceType Type { get; set; } = AssetMaintenanceType.Preventive;

    [Range(0, double.MaxValue, ErrorMessage = "Cost must be zero or more.")]
    public decimal Cost { get; set; }

    [StringLength(150)]
    public string? Vendor { get; set; }

    [Required(ErrorMessage = "Description is required.")]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public AssetRegister? AssetRegister { get; set; }
}