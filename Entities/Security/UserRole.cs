using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class UserRole : BaseEntity
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}