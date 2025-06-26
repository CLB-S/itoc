// For test.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ITOC.Core.Command;

/// <summary>
/// Example of a custom argument type for game player entities
/// </summary>
public class PlayerArgumentType : ArgumentTypeBase
{
    // This would typically come from a player registry or game state
    private readonly List<string> _knownPlayers = new List<string>
    {
        "Player1",
        "Player2",
        "Developer",
        "Admin",
    };

    public override string TypeName => "player";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (string.IsNullOrEmpty(input))
            return false;

        // In a real implementation, this would look up a player entity from the game world
        var playerName = _knownPlayers.FirstOrDefault(p =>
            p.Equals(input, StringComparison.OrdinalIgnoreCase)
        );

        if (playerName != null)
        {
            // Return a player object (for demo we just return the name)
            result = playerName;
            return true;
        }

        return false;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (string.IsNullOrEmpty(currentInput))
            return _knownPlayers;

        return _knownPlayers.Where(p =>
            p.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)
        );
    }
}

/// <summary>
/// Example of a custom argument type for game items
/// </summary>
public class ItemArgumentType : ArgumentTypeBase
{
    // This would typically come from an item registry
    private readonly Dictionary<string, string> _knownItems = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase
    )
    {
        { "sword", "A sharp sword" },
        { "shield", "A sturdy shield" },
        { "potion", "Healing potion" },
        { "arrow", "Bow arrow" },
        { "bow", "Hunting bow" },
        { "gold", "Gold coins" },
    };

    public override string TypeName => "item";

    public override bool TryParse(string input, out object result)
    {
        result = null;

        if (string.IsNullOrEmpty(input))
            return false;

        // In a real implementation, this would look up an item from an item registry
        if (_knownItems.TryGetValue(input, out var _))
        {
            // Return an item object (for demo we just return the item ID)
            result = input;
            return true;
        }

        return false;
    }

    public override IEnumerable<string> GetSuggestions(
        string argName,
        string currentInput,
        object context = null
    )
    {
        if (string.IsNullOrEmpty(currentInput))
            return _knownItems.Keys;

        return _knownItems.Keys.Where(i =>
            i.StartsWith(currentInput, StringComparison.OrdinalIgnoreCase)
        );
    }
}
