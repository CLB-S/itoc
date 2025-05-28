using System.Collections;
using System.Collections.ObjectModel;

namespace ITOC.Core.Registry;

/// <summary>
/// A generic registry for managing game content of a specific type.
/// </summary>
/// <typeparam name="T">The type of content managed by this registry</typeparam>
public class Registry<T> : IEnumerable<KeyValuePair<Identifier, T>>, IFreezable where T : class
{
    private readonly Dictionary<Identifier, T> _entries = new();
    private readonly Dictionary<T, Identifier> _reverseEntries = new();
    private bool _isFrozen;

    /// <summary>
    /// The name of this registry
    /// </summary>
    public Identifier RegistryName { get; }

    /// <summary>
    /// Event raised when an entry is registered
    /// </summary>
    public event EventHandler<RegistryEntryEventArgs<T>> EntryRegistered;

    /// <summary>
    /// Event raised when the registry is frozen
    /// </summary>
    public event EventHandler<RegistryFrozenEventArgs<T>> RegistryFrozen;

    /// <summary>
    /// Whether this registry is frozen (no more entries can be added)
    /// </summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// The number of entries in this registry
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Returns a read-only view of all entries in this registry
    /// </summary>
    public IReadOnlyDictionary<Identifier, T> Entries => new ReadOnlyDictionary<Identifier, T>(_entries);

    /// <summary>
    /// Creates a new registry with the specified name
    /// </summary>
    public Registry(Identifier registryName)
    {
        RegistryName = registryName;
    }

    /// <summary>
    /// Registers an entry with the specified identifier
    /// </summary>
    /// <param name="id">The identifier for this entry</param>
    /// <param name="entry">The entry to register</param>
    /// <returns>The registered entry</returns>
    /// <exception cref="InvalidOperationException">Thrown when the registry is frozen</exception>
    /// <exception cref="ArgumentException">Thrown when the identifier is already registered</exception>
    public T Register(Identifier id, T entry)
    {
        if (_isFrozen)
            throw new InvalidOperationException($"Cannot register entry to frozen registry: {RegistryName}");

        if (_entries.TryGetValue(id, out T existingEntry))
            throw new ArgumentException($"Existing entry {existingEntry} with ID {id} is already registered in {RegistryName}");

        if (_reverseEntries.TryGetValue(entry, out Identifier value))
            throw new ArgumentException($"Entry {entry} is already registered with ID {value} in {RegistryName}");

        _entries[id] = entry;
        _reverseEntries[entry] = id;

        OnEntryRegistered(id, entry);

        return entry;
    }

    /// <summary>
    /// Gets an entry by its identifier
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <returns>The entry, or null if not found</returns>
    public T Get(Identifier id)
    {
        return _entries.TryGetValue(id, out var entry) ? entry : null;
    }

    /// <summary>
    /// Gets an entry by its identifier, or returns the default value if not found
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <param name="defaultValue">The default value to return if the entry is not found</param>
    /// <returns>The entry, or the default value if not found</returns>
    public T GetOrDefault(Identifier id, T defaultValue) => _entries.TryGetValue(id, out var entry) ? entry : defaultValue;

    /// <summary>
    /// Tries to get an entry by its identifier
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <param name="entry">The entry, if found</param>
    /// <returns>True if the entry was found, false otherwise</returns>
    public bool TryGet(Identifier id, out T entry) => _entries.TryGetValue(id, out entry);

    /// <summary>
    /// Gets the identifier of an entry
    /// </summary>
    /// <param name="entry">The entry</param>
    /// <returns>The identifier of the entry, or null if not found</returns>
    public Identifier? GetId(T entry) => _reverseEntries.TryGetValue(entry, out var id) ? id : null;

    /// <summary>
    /// Checks if an entry is registered
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <returns>True if the entry is registered, false otherwise</returns>
    public bool Contains(Identifier id) => _entries.ContainsKey(id);

    /// <summary>
    /// Freezes this registry, preventing further entries from being registered
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
        OnRegistryFrozen();
    }

    /// <summary>
    /// Returns all identifiers in this registry
    /// </summary>
    /// <returns>All identifiers</returns>
    public IEnumerable<Identifier> GetIds() => _entries.Keys;

    /// <summary>
    /// Returns all entries in this registry
    /// </summary>
    /// <returns>All entries</returns>
    public IEnumerable<T> GetEntries() => _entries.Values;

    /// <summary>
    /// Returns all entries in this registry that match the namespace
    /// </summary>
    /// <param name="namespace">The namespace to filter by</param>
    /// <returns>All entries with identifiers in the specified namespace</returns>
    public IEnumerable<T> GetEntriesByNamespace(string @namespace) =>
        _entries.Where(kvp => kvp.Key.Namespace == @namespace).Select(kvp => kvp.Value);

    /// <summary>
    /// Called when an entry is registered
    /// </summary>
    /// <param name="id">The identifier of the entry</param>
    /// <param name="entry">The entry</param>
    protected virtual void OnEntryRegistered(Identifier id, T entry)
    {
        EntryRegistered?.Invoke(this, new RegistryEntryEventArgs<T>(this, id, entry));
    }

    /// <summary>
    /// Called when the registry is frozen
    /// </summary>
    protected virtual void OnRegistryFrozen()
    {
        RegistryFrozen?.Invoke(this, new RegistryFrozenEventArgs<T>(this));
    }

    /// <summary>
    /// Returns an enumerator that iterates through all entries in this registry
    /// </summary>
    public IEnumerator<KeyValuePair<Identifier, T>> GetEnumerator() => Entries.GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through all entries in this registry
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
