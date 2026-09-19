using System.ComponentModel.DataAnnotations;
using CompanyERP.Entities.CompanyBranch;

namespace CompanyERP.ViewModels.CompanyBranch;

public class BranchFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Company is required.")]
    [Display(Name = "Company")]
    public int CompanyId { get; set; }

    [Required(ErrorMessage = "Branch type is required.")]
    [Display(Name = "Branch Type")]
    public int BranchTypeId { get; set; }

    [Required(ErrorMessage = "Branch code is required.")]
    [StringLength(20)]
    [Display(Name = "Branch Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Branch name is required.")]
    [StringLength(150)]
    [Display(Name = "Branch Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Short Name")]
    public string? ShortName { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(20)]
    [Display(Name = "Postal Code")]
    public string? PostalCode { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(30)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Opening Date")]
    public DateTime? OpeningDate { get; set; }

    [Display(Name = "Head Office")]
    public bool IsHeadOffice { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public BranchSettingsFormViewModel Settings { get; set; } = new();

    public Branch ToEntity()
    {
        var branch = new Branch
        {
            Id = Id ?? 0,
            CompanyId = CompanyId,
            BranchTypeId = BranchTypeId,
            Code = Code,
            Name = Name,
            ShortName = ShortName,
            Address = Address,
            City = City,
            State = State,
            PostalCode = PostalCode,
            Country = Country,
            Phone = Phone,
            Email = Email,
            OpeningDate = OpeningDate,
            IsHeadOffice = IsHeadOffice,
            IsActive = IsActive,
            Settings = Settings.ToEntity()
        };

        branch.Settings!.Branch = branch;
        return branch;
    }

    public static BranchFormViewModel FromEntity(Branch branch)
    {
        return new BranchFormViewModel
        {
            Id = branch.Id,
            CompanyId = branch.CompanyId,
            BranchTypeId = branch.BranchTypeId,
            Code = branch.Code,
            Name = branch.Name,
            ShortName = branch.ShortName,
            Address = branch.Address,
            City = branch.City,
            State = branch.State,
            PostalCode = branch.PostalCode,
            Country = branch.Country,
            Phone = branch.Phone,
            Email = branch.Email,
            OpeningDate = branch.OpeningDate,
            IsHeadOffice = branch.IsHeadOffice,
            IsActive = branch.IsActive,
            Settings = branch.Settings is not null
                ? BranchSettingsFormViewModel.FromEntity(branch.Settings)
                : new BranchSettingsFormViewModel { BranchId = branch.Id }
        };
    }
}