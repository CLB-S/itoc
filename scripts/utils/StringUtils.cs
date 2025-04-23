using System;
using System.Text.RegularExpressions;

public static class StringUtils
{
    public static readonly Regex IdRegex = new Regex(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    public static bool IsValidId(string id)
    {
        return IdRegex.IsMatch(id);
    }
}