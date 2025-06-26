using System.Text.RegularExpressions;

namespace ITOC.Core.Utils;

public static class StringUtils
{
    public static readonly Regex IdRegex = new(@"^[a-z0-9_]+$", RegexOptions.Compiled);

    public static bool IsValidId(string id) => IdRegex.IsMatch(id);
}
