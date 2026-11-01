using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class MaintenanceRegressionTests
{
    [Fact]
    public void BoredomShortensIdleWaitWithoutBypassingWalkDisable()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, fullyTransparent: false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"),
                "{\"behavior\":{\"idleSecondsMin\":6,\"idleSecondsMax\":6,\"walkSecondsMin\":10,\"walkSecondsMax\":10}}");
            using var definition = CharacterLoader.LoadDirectory(directory);
            var bored = new PetInstance(definition, new System.Numerics.Vector2(100, 720));
            var rested = new PetInstance(definition, bored.FootPosition);
            var engine = new PetEngine(new TestSurface(), new FixedRandom());
            // Raise boredom before the idle wait is sampled: a bored pet waits half as long.
            for (var i = 0; i < 10000; i++) bored.AdvanceNeeds(.1f, false, 1);
            engine.Tick(bored, .1); engine.Tick(rested, .1);
            engine.Tick(bored, .1); engine.Tick(rested, .1);
            for (var i = 0; i < 40; i++) { engine.Tick(bored, .1); engine.Tick(rested, .1); }
            Assert.Equal(PetState.Walk, bored.State);
            Assert.Equal(PetState.Idle, rested.State);
            engine.DefaultWalkEnabled = false;
            engine.Tick(bored, .1);
            Assert.Equal(PetState.Idle, bored.State);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    [Theory]
    [InlineData(418, 418, 64f)]
    [InlineData(682, 10, 64f)]
    [InlineData(32, 32, float.NaN)]
    public void EffectiveScaleRespectsRuntimePixelLimits(int width, int height, float requested)
    {
        var scale = PetLimits.EffectiveScale(width, height, requested);
        Assert.True(float.IsFinite(scale));
        Assert.InRange(width * scale, 1, PetLimits.MaxRenderedDimension + .001f);
        Assert.InRange(height * scale, 1, PetLimits.MaxRenderedDimension + .001f);
    }

    [Fact]
    public void LoaderAcceptedMaximumWalkSpeedIsNotSilentlyHalved()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), "{\"movement\":{\"walkSpeed\":2000}}");
            using var definition = CharacterLoader.LoadDirectory(directory);
            var pet = new PetInstance(definition, new System.Numerics.Vector2(100, 720));
            pet.StartWalking(FacingDirection.Right);
            new PetEngine(new TestSurface(), new FixedRandom()) { JumpDesire = 0, ClimbDesire = 0 }.Tick(pet, .1);
            Assert.Equal(300f, pet.FootPosition.X, 3);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    [Fact]
    public void FailingInstanceAddedSubscriberRollsBackThePetAndLease()
    {
        var definition = TestSpriteFactory.LoadSpriteOnly("failed-add");
        using var session = new PetSession(new TestSurface(), new FixedRandom());
        session.RegisterDefinition(definition.Id, definition);
        session.InstanceAdded += _ => throw new InvalidOperationException("failed presentation");
        Assert.Throws<InvalidOperationException>(() => session.Create(definition.Id));
        Assert.Empty(session.Instances);
        session.Dispose();
        Assert.True(definition.IsDisposed);
    }
    [Fact]
    public void FailedScanReleasesUncommittedDefinitionsAndPreservesPublishedCatalog()
    {
        var root = TestSpriteFactory.WriteSprite(96, 96, false);
        var loaded = new List<CharacterDefinition>();
        var fail = false;
        using var catalog = new CharacterCatalog(path =>
        {
            if (fail && loaded.Count == 2) throw new InvalidOperationException("interrupted scan");
            var definition = TestSpriteFactory.LoadSpriteOnly("role-" + loaded.Count);
            loaded.Add(definition);
            return definition;
        }, _ => fail ? new[] { "first", "second" } : new[] { "original" });
        try
        {
            catalog.Scan(root);
            fail = true;
            Assert.Throws<InvalidOperationException>(() => catalog.Scan(root));
            Assert.True(loaded[1].IsDisposed);
            Assert.False(loaded[0].IsDisposed);
            Assert.Same(loaded[0], catalog.Definitions.Values.Single());
        }
        finally { catalog.Dispose(); TestSpriteFactory.DeleteFixture(root); }
    }

    [Fact]
    public void DeniedRoleAndRootDoNotInvalidatePublishedCatalog()
    {
        var root = TestSpriteFactory.WriteSprite(96, 96, false);
        var denied = false;
        var denyRoot = false;
        using var catalog = new CharacterCatalog(path => denied
            ? throw new UnauthorizedAccessException("denied")
            : TestSpriteFactory.LoadSpriteOnly("good"),
            _ => denyRoot ? throw new UnauthorizedAccessException("root denied") : new[] { "good" });
        try
        {
            var first = catalog.Scan(root)["good"];
            denied = true;
            Assert.Same(first, catalog.Scan(root)["good"]);
            Assert.Single(catalog.LastFailures);
            denyRoot = true;
            Assert.Same(first, catalog.Scan(root)["good"]);
            Assert.False(first.IsDisposed);
        }
        finally { catalog.Dispose(); TestSpriteFactory.DeleteFixture(root); }
    }

    [Fact]
    public void SameIdReplacementKeepsOldPetUsableAndReleasesBothDefinitions()
    {
        var first = TestSpriteFactory.LoadSpriteOnly("same-id");
        var second = TestSpriteFactory.LoadSpriteOnly("same-id");
        using var session = new PetSession(new TestSurface(), new FixedRandom());
        session.CreateMany(first, 1);
        session.CreateMany(second, 1);
        Assert.False(first.IsDisposed);
        Assert.NotNull(first.SelectFrame(FrameKind.Idle));
        session.Clear();
        Assert.True(first.IsDisposed);
        session.Dispose();
        Assert.True(second.IsDisposed);
    }

    private sealed class TestSurface : IWorldSurface
    {
        public WorldRect WorkingArea => new(0, 0, 1280, 720);
        public bool IsValid => true;
    }

    private sealed class FixedRandom : IRandomSource
    {
        // Idle/walk durations are uniformly sampled from the configured range,
        // so the midpoint keeps the comparison independent of the bounds.
        public double NextDouble() => 0.5;
        public int Next(int minInclusive, int maxExclusive) => minInclusive;
    }
}
