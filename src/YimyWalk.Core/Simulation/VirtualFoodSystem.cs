using System.Numerics;

namespace YimyWalk.Core.Simulation;

public enum VirtualFoodState
{
    Available,
    Claimed,
    Consumed
}

public sealed class VirtualFoodTarget
{
    internal VirtualFoodTarget(Vector2 position) => Position = position;

    public Guid Id { get; } = Guid.NewGuid();
    public Vector2 Position { get; internal set; }
    public VirtualFoodState State { get; internal set; }
    public Guid? ClaimantId { get; internal set; }
    public float VisibleScale { get; internal set; } = 1f;
    internal double AgeSeconds { get; set; }
    internal double ConsumeSeconds { get; set; }
}

/// <summary>
/// Owns program-generated food targets only. It never reads or mutates desktop
/// files, shortcuts, or windows.
/// </summary>
public sealed class VirtualFoodSystem
{
    private const double TargetLifetimeSeconds = 8;
    private const double ConsumeLifetimeSeconds = 0.35;
    private readonly List<VirtualFoodTarget> _targets = new();

    public VirtualFoodSystem(int maxTargets = 32)
        => MaxTargets = Math.Clamp(maxTargets, 1, 256);

    public int MaxTargets { get; }
    public IReadOnlyList<VirtualFoodTarget> Targets => _targets;

    public VirtualFoodTarget Spawn(Vector2 position)
    {
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y))
            throw new ArgumentOutOfRangeException(nameof(position));
        if (_targets.Count >= MaxTargets)
            throw new InvalidOperationException("virtual food target limit reached");
        var target = new VirtualFoodTarget(position);
        _targets.Add(target);
        return target;
    }

    public bool TryClaim(PetInstance pet, Guid targetId)
    {
        ArgumentNullException.ThrowIfNull(pet);
        if (pet.State is not (PetState.Idle or PetState.Walk))
            return false;
        var target = _targets.FirstOrDefault(item => item.Id == targetId);
        if (target is null || target.State != VirtualFoodState.Available) return false;
        if (_targets.Any(item => item.State == VirtualFoodState.Claimed && item.ClaimantId == pet.InstanceId)) return false;
        target.State = VirtualFoodState.Claimed;
        target.ClaimantId = pet.InstanceId;
        pet.FoodTargetPosition = target.Position;
        return true;
    }

    public void Prepare(IReadOnlyCollection<PetInstance> pets, bool defaultWalkEnabled)
    {
        foreach (var pet in pets) pet.FoodTargetPosition = null;
        foreach (var target in _targets.Where(target => target.State == VirtualFoodState.Available))
        {
            var nearest = pets.Where(pet => CanApproach(pet, target, defaultWalkEnabled)
                    && !_targets.Any(other => other.State == VirtualFoodState.Claimed && other.ClaimantId == pet.InstanceId))
                .MinBy(pet => Vector2.DistanceSquared(pet.FootPosition, target.Position));
            if (nearest is not null) TryClaim(nearest, target.Id);
        }
        foreach (var target in _targets.Where(target => target.State == VirtualFoodState.Claimed))
        {
            var pet = pets.FirstOrDefault(pet => pet.InstanceId == target.ClaimantId);
            if (pet is not null && CanApproach(pet, target, defaultWalkEnabled))
                pet.FoodTargetPosition = target.Position;
            else Release(target, pet);
        }
    }

    private static bool CanApproach(PetInstance pet, VirtualFoodTarget target, bool defaultWalkEnabled)
        => pet.State is PetState.Idle or PetState.Walk
            && pet.BehaviorOverrides.ResolveWalkEnabled(defaultWalkEnabled)
            && Math.Abs(pet.FootPosition.Y - target.Position.Y) <= 2f;

    private static void Release(VirtualFoodTarget target, PetInstance? pet)
    {
        target.State = VirtualFoodState.Available;
        target.ClaimantId = null;
        if (pet is not null) pet.FoodTargetPosition = null;
    }

    public void Tick(IEnumerable<PetInstance> pets, double deltaSeconds, bool defaultWalkEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(pets);
        var delta = double.IsFinite(deltaSeconds) ? Math.Clamp(deltaSeconds, 0, PetEngine.MaxDeltaSeconds) : 0;
        if (delta <= 0) return;
        var byId = pets.ToDictionary(pet => pet.InstanceId);
        foreach (var target in _targets.ToArray())
        {
            target.AgeSeconds += delta;
            if (target.State != VirtualFoodState.Consumed && target.AgeSeconds >= TargetLifetimeSeconds)
            {
                if (byId.TryGetValue(target.ClaimantId ?? Guid.Empty, out var expiredPet)) expiredPet.FoodTargetPosition = null;
                _targets.Remove(target);
                continue;
            }
            if (target.State == VirtualFoodState.Consumed)
            {
                target.ConsumeSeconds += delta;
                target.VisibleScale = Math.Max(0, 1f - (float)(target.ConsumeSeconds / ConsumeLifetimeSeconds));
                if (target.ConsumeSeconds >= ConsumeLifetimeSeconds) _targets.Remove(target);
                continue;
            }
            if (target.State != VirtualFoodState.Claimed || target.ClaimantId is not { } claimantId
                || !byId.TryGetValue(claimantId, out var pet)
                || !CanApproach(pet, target, defaultWalkEnabled))
            {
                if (target.State == VirtualFoodState.Claimed)
                {
                    Release(target, byId.GetValueOrDefault(target.ClaimantId ?? Guid.Empty));
                }
                continue;
            }

            var offset = target.Position - pet.FootPosition;
            var distance = offset.Length();
            if (distance <= 2f)
            {
                pet.Feed(0.5f);
                pet.FoodTargetPosition = null;
                target.State = VirtualFoodState.Consumed;
                target.ClaimantId = pet.InstanceId;
                target.VisibleScale = 1f;
                continue;
            }

            // Motion belongs to PetEngine. This system only owns target intent
            // and consumption, never pet position, velocity or movement state.
        }
    }

    public void Clear() => _targets.Clear();
}
