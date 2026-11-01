using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Resources;

namespace YimyWalk.Core.Simulation;

public sealed class PetSession : IDisposable
{
    private readonly Dictionary<Guid, (PetInstance Pet, DefinitionLease.LeaseHandle Lease)> _instances = new();
    private readonly Dictionary<string, DefinitionLease> _definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly PetEngine _engine;
    private bool _disposed;
    private IReadOnlyCollection<PetInstance>? _snapshot;

    public PetSession(IWorldSurface surface, IRandomSource random, int? hardLimit = null)
    {
        _engine = new PetEngine(surface, random);
        HardLimit = hardLimit;
    }

    public int? HardLimit { get; }
    public bool IsPaused { get; private set; }
    public bool HungerEnabled { get => _engine.HungerEnabled; set => _engine.HungerEnabled = value; }
    public float NeedRate { get => _engine.NeedRate; set => _engine.NeedRate = float.IsFinite(value) ? Math.Clamp(value, 0f, 4f) : 1f; }
    public bool DefaultWalkEnabled { get => _engine.DefaultWalkEnabled; set => _engine.DefaultWalkEnabled = value; }
    public float ActivityRate { get => _engine.ActivityRate; set => _engine.ActivityRate = float.IsFinite(value) ? Math.Clamp(value, 0.1f, 4f) : 1f; }
    public float JumpDesire { get => _engine.JumpDesire; set => _engine.JumpDesire = ClampDesire(value); }
    public float ClimbDesire { get => _engine.ClimbDesire; set => _engine.ClimbDesire = ClampDesire(value); }
    public float WindowJumpDesire { get => _engine.WindowJumpDesire; set => _engine.WindowJumpDesire = ClampDesire(value); }
    public float GlobalScale { get => _engine.GlobalScale; set => _engine.GlobalScale = ClampScale(value); }
    public int ActiveCount => _instances.Count;
    public IWorldSurface Surface => _engine.Surface;
    public VirtualFoodSystem VirtualFood { get; } = new();
    public IReadOnlyCollection<PetInstance> Instances => _snapshot ??= Array.AsReadOnly(_instances.Values.Select(value => value.Pet).ToArray());

    public event Action<PetInstance>? InstanceAdded;
    public event Action<PetInstance>? InstanceRemoved;
    public event Action<PetInstance, Exception>? InstanceFaulted;

    public void RegisterDefinition(string id, CharacterDefinition definition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(definition);
        var lease = new DefinitionLease(definition);
        if (_definitions.TryGetValue(id, out var previous))
            previous.Dispose();
        _definitions[id] = lease;
    }

    public bool ReplaceDefinition(string id, CharacterDefinition definition)
    {
        if (!_definitions.ContainsKey(id))
        {
            RegisterDefinition(id, definition);
            return true;
        }
        RegisterDefinition(id, definition);
        return true;
    }

    public bool RemoveDefinition(string id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_definitions.Remove(id, out var lease)) return false;
        lease.Dispose();
        return true;
    }

    public PetInstance Create(string id, Vector2? footPosition = null)
    {
        if (!_definitions.TryGetValue(id, out var lease))
            throw new KeyNotFoundException($"Character '{id}' is not registered.");
        return Create(lease, footPosition ?? DefaultSpawnPosition());
    }

    public IReadOnlyList<PetInstance> CreateMany(CharacterDefinition definition, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (count <= 0) return Array.Empty<PetInstance>();
        var lease = GetOrAddLease(definition);
        var available = HardLimit is null ? count : Math.Max(0, HardLimit.Value - ActiveCount);
        var result = new List<PetInstance>(Math.Min(count, available));
        for (var i = 0; i < available && i < count; i++)
            result.Add(Create(lease, DefaultSpawnPosition(i)));
        return result;
    }

    public bool Remove(Guid instanceId)
    {
        if (!_instances.Remove(instanceId, out var entry)) return false;
        _snapshot = null;
        entry.Lease.Dispose();
        InstanceRemoved?.Invoke(entry.Pet);
        return true;
    }

    public void Clear()
    {
        foreach (var id in _instances.Keys.ToArray())
            Remove(id);
        VirtualFood.Clear();
    }

    public void Tick(double deltaSeconds)
    {
        if (IsPaused) return;
        var pets = Instances;
        VirtualFood.Prepare(pets, DefaultWalkEnabled);
        foreach (var pet in pets)
        {
            try { _engine.Tick(pet, deltaSeconds); }
            catch (Exception error) when (InstanceFaulted is not null && error is not OutOfMemoryException)
            {
                Remove(pet.InstanceId);
                InstanceFaulted?.Invoke(pet, error);
            }
        }
        VirtualFood.Tick(pets, deltaSeconds, DefaultWalkEnabled);
    }

    public void Pause() => IsPaused = true;
    public void Resume() => IsPaused = false;

    public int FeedAll()
    {
        var fed = 0;
        foreach (var pet in _instances.Values.Select(value => value.Pet))
            if (pet.Feed()) fed++;
        return fed;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Clear();
        VirtualFood.Clear();
        foreach (var lease in _definitions.Values)
            lease.Dispose();
        _definitions.Clear();
        InstanceAdded = null;
        InstanceRemoved = null;
        InstanceFaulted = null;
    }

    private DefinitionLease GetOrAddLease(CharacterDefinition definition)
    {
        var existing = _definitions.Values.FirstOrDefault(lease => ReferenceEquals(lease.Definition, definition));
        if (existing is not null) return existing;
        var lease = new DefinitionLease(definition);
        if (_definitions.TryGetValue(definition.Id, out var previous)) previous.Dispose();
        _definitions[definition.Id] = lease;
        return lease;
    }

    private PetInstance Create(DefinitionLease lease, Vector2 position)
    {
        if (HardLimit is not null && ActiveCount >= HardLimit.Value)
            throw new InvalidOperationException("Configured pet session limit reached.");
        var pet = new PetInstance(lease.Definition, position);
        var handle = lease.AcquireInstance();
        _instances.Add(pet.InstanceId, (pet, handle));
        _snapshot = null;
        try { InstanceAdded?.Invoke(pet); }
        catch { _instances.Remove(pet.InstanceId); _snapshot = null; handle.Dispose(); throw; }
        return pet;
    }

    private Vector2 DefaultSpawnPosition(int offset = 0)
    {
        var area = Surface.SpawnArea;
        var x = area.Left + Math.Min(area.Width, 80 + (offset % 10) * 50);
        return new Vector2(x, area.Top - 32);
    }

    private static float ClampDesire(float value)
        => float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;

    private static float ClampScale(float value)
        => float.IsFinite(value) ? Math.Clamp(value, PetInstance.MinScale, PetInstance.MaxScale) : 1f;
}
