using System.Numerics;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class BehaviorPacingTests
{
    [Fact]
    public void AutonomousWalkStopsAfterConfiguredDurationAndReturnsToIdle()
    {
        using var definition = Configured(idleSeconds: 5f, walkSeconds: 0.3f);
        var pet = new PetInstance(definition, new Vector2(200, 720));
        var engine = Engine();

        for (var i = 0; i < 30 && pet.State != PetState.Idle; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Idle, pet.State);

        pet.StartWalking(FacingDirection.Right, 0.3f);
        for (var i = 0; i < 6; i++) engine.Tick(pet, 0.1);

        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(0, pet.Velocity.X);
    }

    [Fact]
    public void IdleWaitBeforeAutonomousWalkUsesConfiguredBounds()
    {
        using var definition = Configured(idleSeconds: 0.2f, walkSeconds: 5f);
        var pet = new PetInstance(definition, new Vector2(200, 720));
        var engine = Engine();

        for (var i = 0; i < 30 && pet.State != PetState.Idle; i++) engine.Tick(pet, 0.1);
        Assert.Equal(PetState.Idle, pet.State);

        var elapsed = 0f;
        for (var i = 0; i < 20 && pet.State != PetState.Walk; i++)
        {
            engine.Tick(pet, 0.02);
            elapsed += 0.02f;
        }

        Assert.Equal(PetState.Walk, pet.State);
        Assert.InRange(elapsed, 0.18f, 0.5f);
    }

    [Fact]
    public void ClaimedFoodSurvivesLongerThanTheUnclaimedLifetime()
    {
        var pet = new PetInstance(TestSpriteFactory.LoadSpriteOnly("slow-food"), new Vector2(0, 720));
        var foods = new VirtualFoodSystem();
        var target = foods.Spawn(new Vector2(600, 720));
        pet.StartWalking(FacingDirection.Right);
        Assert.True(foods.TryClaim(pet, target.Id));

        for (var i = 0; i < 150; i++) foods.Tick(new[] { pet }, 0.1);

        Assert.Contains(target, foods.Targets);
        Assert.Equal(VirtualFoodState.Claimed, target.State);
    }

    private static CharacterDefinition Configured(float idleSeconds, float walkSeconds)
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false);
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                behavior = new
                {
                    idleSecondsMin = idleSeconds,
                    idleSecondsMax = idleSeconds,
                    walkSecondsMin = walkSeconds,
                    walkSecondsMax = walkSeconds
                }
            });
            File.WriteAllText(Path.Combine(directory, "character.json"), json);
            return CharacterLoader.LoadDirectory(directory);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    private static PetEngine Engine() => new(new Surface(), new FixedRandom())
    {
        JumpDesire = 0,
        WindowJumpDesire = 0,
        ClimbDesire = 0
    };

    private sealed class Surface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
