using System.Collections;

namespace ITOC.Core.Registry;

/// <summary>
/// A tag that can be applied to registry entries to group them together
/// </summary>
/// <typeparam name="T">The type of content managed by the registry</typeparam>
public class RegistryTag<T> : IEnumerable<T> where T : class
{
    private readonly HashSet<Identifier> _entries = new();
    private readonly Registry<T> _registry;

    /// <summary>
    /// The identifier of this tag
    /// </summary>
    public Identifier Id { get; }

    /// <summary>
    /// The number of entries in this tag
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Creates a new tag with the specified identifier
    /// </summary>
    /// <param name="registry">The registry that contains the entries</param>
    /// <param name="id">The identifier of this tag</param>
    public RegistryTag(Registry<T> registry, Identifier id)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Id = id;
    }

    /// <summary>
    /// Adds an entry to this tag
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <returns>True if the entry was added, false if it was already in this tag</returns>
    /// <exception cref="ArgumentException">Thrown when the registry does not contain an entry with the specified identifier</exception>
    public bool Add(Identifier id)
    {
        if (!_registry.Contains(id))
            throw new ArgumentException($"Registry {_registry.RegistryName} does not contain an entry with ID {id}", nameof(id));

        return _entries.Add(id);
    }

    /// <summary>
    /// Adds multiple entries to this tag
    /// </summary>
    /// <param name="ids">The identifiers of the entries</param>
    /// <exception cref="ArgumentException">Thrown when the registry does not contain an entry with one of the specified identifiers</exception>
    public void AddRange(IEnumerable<Identifier> ids)
    {
        foreach (var id in ids)
            Add(id);
    }

    /// <summary>
    /// Removes an entry from this tag
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <returns>True if the entry was removed, false if it wasn't in this tag</returns>
    public bool Remove(Identifier id)
    {
        return _entries.Remove(id);
    }

    /// <summary>
    /// Checks if this tag contains an entry
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <returns>True if this tag contains the entry, false otherwise</returns>
    public bool Contains(Identifier id)
    {
        return _entries.Contains(id);
    }

    /// <summary>
    /// Gets all entries in this tag
    /// </summary>
    /// <returns>All entries in this tag</returns>
    public IEnumerable<T> GetEntries()
    {
        return _entries.Select(_registry.Get).Where(entry => entry != null);
    }

    /// <summary>
    /// Gets all identifiers in this tag
    /// </summary>
    /// <returns>All identifiers in this tag</returns>
    public IEnumerable<Identifier> GetIds()
    {
        return _entries.ToList();
    }

    /// <summary>
    /// Returns an enumerator that iterates through all entries in this tag
    /// </summary>
    public IEnumerator<T> GetEnumerator()
    {
        return GetEntries().GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through all entries in this tag
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
