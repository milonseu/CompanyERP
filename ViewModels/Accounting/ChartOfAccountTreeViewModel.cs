using CompanyERP.Entities.Accounting;

namespace CompanyERP.ViewModels.Accounting;

/// <summary>
/// One row of the Chart of Accounts tree. Carries the level metadata the seeded
/// four layer structure implies so the view never has to re-derive it from Depth.
/// </summary>
public class ChartOfAccountTreeRow
{
    public ChartOfAccount Account { get; set; } = null!;

    public int Id => Account.Id;

    public string AccountCode => Account.AccountCode;

    public string AccountName => Account.AccountName;

    public AccountType AccountType => Account.AccountType;

    public AccountNormalBalance NormalBalance => Account.NormalBalance;

    public decimal OpeningBalance => Account.OpeningBalance;

    public string? Description => Account.Description;

    public bool IsActive => Account.IsActive;

    public bool IsPostable => Account.IsPostable;

    public int? ParentId => Account.ParentId;

    /// <summary>Zero based depth from the service tree.</summary>
    public int Depth { get; set; }

    /// <summary>One based level number shown to the user (1 = Class).</summary>
    public int Level => Depth + 1;

    /// <summary>Only leaf rows can be posted to, so only they get edit/delete actions.</summary>
    public bool HasChildren { get; set; }

    /// <summary>True when this row is a parent that the user can expand.</summary>
    public bool IsExpandable => HasChildren;

    /// <summary>Class / Group / Sub-Group / Leaf, matching the seeded four layer design.</summary>
    public string LevelName => Level switch
    {
        1 => "Class",
        2 => "Group",
        3 => "Sub-Group",
        _ => "Leaf"
    };

    public string TypeCssClass => AccountType switch
    {
        AccountType.Asset => "coa-type-asset",
        AccountType.Liability => "coa-type-liability",
        AccountType.Equity => "coa-type-equity",
        AccountType.Revenue => "coa-type-revenue",
        _ => "coa-type-expense"
    };

    public string TypeIcon => AccountType switch
    {
        AccountType.Asset => "bi-box-seam",
        AccountType.Liability => "bi-arrow-down-circle",
        AccountType.Equity => "bi-pie-chart",
        AccountType.Revenue => "bi-graph-up-arrow",
        _ => "bi-graph-down-arrow"
    };

    public string LevelBadgeCss => Level switch
    {
        1 => "coa-level-1",
        2 => "coa-level-2",
        3 => "coa-level-3",
        _ => "coa-level-4"
    };
}

public class ChartOfAccountTypeCount
{
    public AccountType AccountType { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }

    public string Icon { get; set; } = string.Empty;

    public string CssClass { get; set; } = string.Empty;
}

/// <summary>
/// A parent option for the account form. Carries the parent's type so the view can
/// mirror it onto the child and keep the two in sync, matching the service rule that
/// an account type must match its parent's class.
/// </summary>
public class ChartOfAccountParentOption
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public int AccountType { get; set; }

    public int NormalBalance { get; set; }
}

public class ChartOfAccountIndexViewModel
{
    public List<ChartOfAccountTreeRow> Rows { get; set; } = [];

    public List<ChartOfAccountTypeCount> TypeCounts { get; set; } = [];

    public int TotalCount => Rows.Count;

    public int PostableCount => Rows.Count(r => r.IsPostable);

    public int ParentCount => Rows.Count(r => r.HasChildren);

    public ChartOfAccountIndexViewModel Build(IReadOnlyList<(ChartOfAccount Account, int Depth)> tree)
    {
        // Ids that appear as someone's ParentId. A node is a parent iff its id is in
        // this set, which is exact and avoids scanning the whole tree per row.
        var parentIds = tree
            .Where(t => t.Account.ParentId.HasValue)
            .Select(t => t.Account.ParentId!.Value)
            .ToHashSet();

        Rows = tree.Select(t => new ChartOfAccountTreeRow
        {
            Account = t.Account,
            Depth = t.Depth,
            HasChildren = parentIds.Contains(t.Account.Id)
        }).ToList();

        TypeCounts = Enum.GetValues<AccountType>()
            .Select(type => new ChartOfAccountTypeCount
            {
                AccountType = type,
                Name = type.ToString(),
                Count = Rows.Count(r => r.AccountType == type),
                Icon = type switch
                {
                    AccountType.Asset => "bi-box-seam",
                    AccountType.Liability => "bi-arrow-down-circle",
                    AccountType.Equity => "bi-pie-chart",
                    AccountType.Revenue => "bi-graph-up-arrow",
                    _ => "bi-graph-down-arrow"
                },
                CssClass = type switch
                {
                    AccountType.Asset => "coa-type-asset",
                    AccountType.Liability => "coa-type-liability",
                    AccountType.Equity => "coa-type-equity",
                    AccountType.Revenue => "coa-type-revenue",
                    _ => "coa-type-expense"
                }
            })
            .ToList();

        return this;
    }
}
