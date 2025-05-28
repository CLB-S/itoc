using System;
using System.Collections.Generic;

namespace ITOC.Command;

/// <summary>
/// Interface for objects that can have permissions
/// </summary>
public interface IPermissionHolder
{
    /// <summary>
    /// Checks if this object has a specific permission
    /// </summary>
    /// <param name="permission">The permission to check</param>
    /// <returns>True if the object has the permission, false otherwise</returns>
    bool HasPermission(string permission);
}

/// <summary>
/// Basic implementation of a permission holder
/// </summary>
public class SimplePermissionHolder : IPermissionHolder
{
    private readonly HashSet<string> _permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new permission holder with the given permissions
    /// </summary>
    /// <param name="permissions">The permissions to grant</param>
    public SimplePermissionHolder(params string[] permissions)
    {
        if (permissions != null)
            foreach (var permission in permissions)
                _permissions.Add(permission);
    }

    /// <summary>
    /// Adds a permission to this holder
    /// </summary>
    /// <param name="permission">The permission to add</param>
    public void AddPermission(string permission)
    {
        if (!string.IsNullOrEmpty(permission))
            _permissions.Add(permission);
    }

    /// <summary>
    /// Removes a permission from this holder
    /// </summary>
    /// <param name="permission">The permission to remove</param>
    public void RemovePermission(string permission)
    {
        _permissions.Remove(permission);
    }

    /// <summary>
    /// Checks if this holder has the specified permission
    /// </summary>
    public bool HasPermission(string permission)
    {
        if (string.IsNullOrEmpty(permission))
            return true;

        if (_permissions.Contains("*"))
            return true;

        return _permissions.Contains(permission);
    }
}
