using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Asset;

public class AssetDocument : BaseEntity
{
    [Required]
    public int AssetRegisterId { get; set; }

    [Required(ErrorMessage = "Document name is required.")]
    [StringLength(150)]
    [Display(Name = "Document Name")]
    public string DocumentName { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "File Name")]
    public string? FileName { get; set; }

    [Required(ErrorMessage = "Upload date is required.")]
    [Display(Name = "Uploaded On")]
    [DataType(DataType.Date)]
    public DateTime UploadedDate { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Note { get; set; }

    public AssetRegister? AssetRegister { get; set; }
}