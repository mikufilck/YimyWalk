using System.Numerics;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class FoodMotionRegressionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PausingOrDisablingWalkingPreventsFoodMovement(bool pause)
    {
        using var session = CreateSession();
        var pet = session.Instances.Single();
        session.VirtualFood.Spawn(new Vector2(500, 720));
        if (pause) session.Pause();
        else session.DefaultWalkEnabled = false;
        var position = pet.FootPosition;
        for (var i = 0; i < 20; i++) session.Tick(0.1);
        Assert.Equal(position, pet.FootPosition);
        Assert.NotEqual(PetState.Walk, pet.State);
    }

    [Fact]
    public void FoodApproachUsesOnlyWalkSpeedAndFinishesConsumption()
    {
        using var session = CreateSession();
        var pet = session.Instances.Single();
        session.VirtualFood.Spawn(new Vector2(160, 720));
        session.Tick(0.1);
        Assert.Equal(106f, pet.FootPosition.X, 3);
        for (var i = 0; i < 14; i++) session.Tick(0.1);
        Assert.Equal(PetState.Eat, pet.State);
        Assert.Empty(session.VirtualFood.Targets);
    }

    [Fact]
    public void InstanceWalkOverrideAndDragReleaseTheFoodClaim()
    {
        using var session = CreateSession();
        var pet = session.Instances.Single();
        var target = session.VirtualFood.Spawn(new Vector2(500, 720));
        session.Tick(0.1);
        pet.BehaviorOverrides = PetBehaviorOverrides.Inherit with { WalkOverride = BooleanOverride.Disabled };
        var position = pet.FootPosition;
        session.Tick(0.1);
        Assert.Equal(position, pet.FootPosition);
        Assert.Null(target.ClaimantId);
        pet.BehaviorOverrides = PetBehaviorOverrides.Inherit;
        session.Tick(0.1);
        new PetEngine(session.Surface, new FixedRandom()).BeginDrag(pet);
        session.Tick(0.1);
        Assert.Null(target.ClaimantId);
        Assert.Equal(PetState.Dragged, pet.State);
    }

    [Fact]
    public void DifferentFloorAndZeroSpeedTargetsExpireWithoutTeleportation()
    {
        using var session = CreateSession();
        var pet = session.Instances.Single();
        var position = pet.FootPosition;
        session.DefaultWalkEnabled = false;
        session.VirtualFood.Spawn(new Vector2(100, 200));
        for (var i = 0; i < 90; i++) session.Tick(0.1);
        Assert.Empty(session.VirtualFood.Targets);
        Assert.Equal(position, pet.FootPosition);
    }

    private static PetSession CreateSession()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("food-regression");
        var session = new PetSession(new Surface(), new FixedRandom());
        session.RegisterDefinition(definition.Id, definition);
        session.Create(definition.Id, new Vector2(100, 720));
        session.Tick(0.1);
        return session;
    }
    private sealed class Surface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }
    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
