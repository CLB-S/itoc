using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ITOC.Libs.Registry;

namespace ITOC;

/// <summary>
/// Central manager for all registries in the game.
/// Handles registry creation, lookup, and lifecycle management.
/// </summary>
public class RegistryManager
{
    private static readonly Lazy<RegistryManager> _instance = new(CreateInstance);
    private readonly Dictionary<Identifier, object> _registries = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _isRegistryCreationFrozen;

    /// <summary>
    /// The singleton instance of the registry manager
    /// </summary>
    public static RegistryManager Instance => _instance.Value;

    /// <summary>
    /// Whether new registries can be created
    /// </summary>
    public bool IsRegistryCreationFrozen => _isRegistryCreationFrozen;

    /// <summary>
    /// Built-in registry keys
    /// </summary>
    public static class Keys
    {
        public static readonly Identifier Blocks = new(Identifier.ItocNamespace, "blocks");
        public static readonly Identifier Items = new(Identifier.ItocNamespace, "items");
        public static readonly Identifier Biomes = new(Identifier.ItocNamespace, "biomes");
    }

    private RegistryManager()
    {
    }

    private static RegistryManager CreateInstance()
    {
        var instance = new RegistryManager();

        // Create built-in registries
        instance.CreateRegistry<Block>(Keys.Blocks);
        instance.CreateRegistry<IItem>(Keys.Items);
        instance.CreateRegistry<Biome>(Keys.Biomes);

        return instance;
    }

    /// <summary>
    /// Creates a new registry with the specified key and type
    /// </summary>
    /// <typeparam name="T">The type of content managed by the registry</typeparam>
    /// <param name="key">The key for the registry</param>
    /// <returns>The created registry</returns>
    /// <exception cref="InvalidOperationException">Thrown when registry creation is frozen</exception>
    /// <exception cref="ArgumentException">Thrown when a registry with the same key already exists</exception>
    public Registry<T> CreateRegistry<T>(Identifier key) where T : class
    {
        if (_isRegistryCreationFrozen)
            throw new InvalidOperationException("Registry creation is frozen");

        _lock.EnterWriteLock();
        try
        {
            if (_registries.ContainsKey(key))
                throw new ArgumentException($"Registry with key {key} already exists");

            var registry = new Registry<T>(key);
            _registries.Add(key, registry);
            return registry;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets a registry by its key
    /// </summary>
    /// <typeparam name="T">The type of content managed by the registry</typeparam>
    /// <param name="key">The key of the registry</param>
    /// <returns>The registry</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no registry with the specified key exists</exception>
    /// <exception cref="InvalidCastException">Thrown when the registry is of a different type</exception>
    public Registry<T> GetRegistry<T>(Identifier key) where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (!_registries.TryGetValue(key, out var registry))
                throw new KeyNotFoundException($"No registry found with key {key}");

            if (registry is Registry<T> typedRegistry)
                return typedRegistry;

            throw new InvalidCastException($"Registry with key {key} is not of type Registry<{typeof(T).Name}>");
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Tries to get a registry by its key
    /// </summary>
    /// <typeparam name="T">The type of content managed by the registry</typeparam>
    /// <param name="key">The key of the registry</param>
    /// <param name="registry">The registry, if found</param>
    /// <returns>True if the registry was found, false otherwise</returns>
    public bool TryGetRegistry<T>(Identifier key, out Registry<T> registry) where T : class
    {
        _lock.EnterReadLock();
        try
        {
            if (_registries.TryGetValue(key, out var obj) && obj is Registry<T> typedRegistry)
            {
                registry = typedRegistry;
                return true;
            }

            registry = null;
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Checks if a registry with the specified key exists
    /// </summary>
    /// <param name="key">The key of the registry</param>
    /// <returns>True if the registry exists, false otherwise</returns>
    public bool HasRegistry(Identifier key)
    {
        _lock.EnterReadLock();
        try
        {
            return _registries.ContainsKey(key);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all registries
    /// </summary>
    /// <returns>All registries</returns>
    public IEnumerable<object> GetAllRegistries()
    {
        _lock.EnterReadLock();
        try
        {
            return _registries.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Gets all registry keys
    /// </summary>
    /// <returns>All registry keys</returns>
    public IEnumerable<Identifier> GetAllRegistryKeys()
    {
        _lock.EnterReadLock();
        try
        {
            return _registries.Keys.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Freezes registry creation, preventing new registries from being created
    /// </summary>
    public void FreezeRegistryCreation()
    {
        _isRegistryCreationFrozen = true;
    }

    /// <summary>
    /// Freezes all registries, preventing new entries from being registered
    /// </summary>
    public void FreezeAllRegistries()
    {
        _lock.EnterReadLock();
        try
        {
            foreach (var registry in _registries.Values)
            {
                if (registry is IFreezable freezable)
                    freezable.Freeze();
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
