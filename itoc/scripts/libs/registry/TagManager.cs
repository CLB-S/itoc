using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ITOC.Libs.Registry;

/// <summary>
/// Manages tags for a registry
/// </summary>
/// <typeparam name="T">The type of content managed by the registry</typeparam>
public class TagManager<T> where T : class
{
    private readonly Registry<T> _registry;
    private readonly Dictionary<Identifier, RegistryTag<T>> _tags = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _isFrozen;

    /// <summary>
    /// Whether tag creation is frozen
    /// </summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// Creates a new tag manager for the specified registry
    /// </summary>
    /// <param name="registry">The registry</param>
    public TagManager(Registry<T> registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Creates a new tag with the specified identifier
    /// </summary>
    /// <param name="id">The identifier of the tag</param>
    /// <returns>The created tag</returns>
    /// <exception cref="InvalidOperationException">Thrown when tag creation is frozen</exception>
    /// <exception cref="ArgumentException">Thrown when a tag with the same identifier already exists</exception>
    public RegistryTag<T> CreateTag(Identifier id)
    {
        if (_isFrozen)
            throw new InvalidOperationException("Tag creation is frozen");

        _lock.EnterWriteLock();
        try
        {
            if (_tags.ContainsKey(id))
                throw new ArgumentException($"Tag with ID {id} already exists");

            var tag = new RegistryTag<T>(_registry, id);
            _tags[id] = tag;
            return tag;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Creates a new tag with the specified identifier
    /// </summary>
    /// <param name="id">The identifier of the tag as a string</param>
    /// <returns>The created tag</returns>
    public RegistryTag<T> CreateTag(string id) => CreateTag(new Identifier(id));

    /// <summary>
    /// Gets a tag by its identifier
    /// </summary>
    /// <param name="id">The identifier of the tag</param>
    /// <returns>The tag</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no tag with the specified identifier exists</exception>
    public RegistryTag<T> GetTag(Identifier id)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_tags.TryGetValue(id, out var tag))
                throw new KeyNotFoundException($"No tag found with ID {id}");

            return tag;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets a tag by its identifier
    /// </summary>
    /// <param name="id">The identifier of the tag as a string</param>
    /// <returns>The tag</returns>
    public RegistryTag<T> GetTag(string id) => GetTag(new Identifier(id));

    /// <summary>
    /// Gets a tag by its identifier, or creates it if it doesn't exist
    /// </summary>
    /// <param name="id">The identifier of the tag</param>
    /// <returns>The tag</returns>
    /// <exception cref="InvalidOperationException">Thrown when tag creation is frozen and the tag doesn't exist</exception>
    public RegistryTag<T> GetOrCreateTag(Identifier id)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_tags.TryGetValue(id, out var tag))
                return tag;

            _lock.EnterWriteLock();
            try
            {
                if (_isFrozen)
                    throw new InvalidOperationException("Tag creation is frozen");

                tag = new RegistryTag<T>(_registry, id);
                _tags[id] = tag;
                return tag;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    /// <summary>
    /// Gets a tag by its identifier, or creates it if it doesn't exist
    /// </summary>
    /// <param name="id">The identifier of the tag as a string</param>
    /// <returns>The tag</returns>
    public RegistryTag<T> GetOrCreateTag(string id) => GetOrCreateTag(new Identifier(id));

    /// <summary>
    /// Tries to get a tag by its identifier
    /// </summary>
    /// <param name="id">The identifier of the tag</param>
    /// <param name="tag">The tag, if found</param>
    /// <returns>True if the tag was found, false otherwise</returns>
    public bool TryGetTag(Identifier id, out RegistryTag<T> tag)
    {
        _lock.EnterReadLock();
        try
        {
            return _tags.TryGetValue(id, out tag);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Tries to get a tag by its identifier
    /// </summary>
    /// <param name="id">The identifier of the tag as a string</param>
    /// <param name="tag">The tag, if found</param>
    /// <returns>True if the tag was found, false otherwise</returns>
    public bool TryGetTag(string id, out RegistryTag<T> tag) => TryGetTag(new Identifier(id), out tag);

    /// <summary>
    /// Gets all tags
    /// </summary>
    /// <returns>All tags</returns>
    public IEnumerable<RegistryTag<T>> GetAllTags()
    {
        _lock.EnterReadLock();
        try
        {
            return _tags.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Freezes tag creation, preventing new tags from being created
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
    }
}
