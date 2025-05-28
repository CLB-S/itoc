using ITOC.Core.Command;

namespace ITOC.Test.Command;

/// <summary>
/// Mock permission holder for testing permissions
/// </summary>
public class TestPermissionHolder : IPermissionHolder
{
    private readonly HashSet<string> _permissions;

    public TestPermissionHolder(params string[] permissions)
    {
        _permissions = new(permissions);
    }

    public bool HasPermission(string permission)
    {
        return string.IsNullOrEmpty(permission) || _permissions.Contains(permission);
    }
}