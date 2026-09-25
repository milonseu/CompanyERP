using CompanyERP.Entities.Common;

namespace CompanyERP.Entities.Security;

public class UserPermission : BaseEntity
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }

    public User? User { get; set; }
    public Permission? Permission { get; set; }
}