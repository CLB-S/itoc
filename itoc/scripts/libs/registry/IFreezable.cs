namespace ITOC.Libs.Registry;

/// <summary>
/// Interface for objects that can be frozen (made immutable)
/// </summary>
public interface IFreezable
{
    /// <summary>
    /// Whether this object is frozen
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>
    /// Freezes this object, preventing further modifications
    /// </summary>
    void Freeze();
}
