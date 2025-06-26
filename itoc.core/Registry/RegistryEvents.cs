namespace ITOC.Core.Registry;

/// <summary>
/// Event arguments for when an entry is registered to a registry
/// </summary>
/// <typeparam name="T">The type of content managed by the registry</typeparam>
public class RegistryEntryEventArgs<T> : EventArgs
    where T : class
{
    /// <summary>
    /// The registry
    /// </summary>
    public Registry<T> Registry { get; }

    /// <summary>
    /// The identifier of the entry
    /// </summary>
    public Identifier Id { get; }

    /// <summary>
    /// The entry
    /// </summary>
    public T Entry { get; }

    /// <summary>
    /// Creates new registry entry event arguments
    /// </summary>
    /// <param name="registry">The registry</param>
    /// <param name="id">The identifier of the entry</param>
    /// <param name="entry">The entry</param>
    public RegistryEntryEventArgs(Registry<T> registry, Identifier id, T entry)
    {
        Registry = registry;
        Id = id;
        Entry = entry;
    }
}

/// <summary>
/// Event arguments for when a registry is frozen
/// </summary>
/// <typeparam name="T">The type of content managed by the registry</typeparam>
public class RegistryFrozenEventArgs<T> : EventArgs
    where T : class
{
    /// <summary>
    /// The registry
    /// </summary>
    public Registry<T> Registry { get; }

    /// <summary>
    /// Creates new registry frozen event arguments
    /// </summary>
    /// <param name="registry">The registry</param>
    public RegistryFrozenEventArgs(Registry<T> registry) => Registry = registry;
}
