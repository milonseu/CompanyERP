using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CompanyERP;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString());
        var attribute = member.FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>();
        return attribute?.GetName() ?? value.ToString();
    }
}