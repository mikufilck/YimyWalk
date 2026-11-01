using System.Numerics;
using System.Security.Cryptography;
using YimyWalk.Core.Characters;
using YimyWalk.Core.Simulation;

namespace YimyWalk.Core.Tests;

public sealed class AnimationRegressionTests
{
    [Fact]
    public void CustomSequencesHoldExpressionsAndReturnThroughNeutralWalkPose()
    {
        using var definition = Configured();
        Assert.Equal(new[] { 3, 4, 5, 4, 3 }, Enumerable.Range(0, 5)
            .Select(i => definition.SelectFrame(FrameKind.WalkRight, i / 8f + .001f).Index));
        Assert.Equal(0, definition.SelectFrame(FrameKind.Idle, 123.45f).Index);
        Assert.Equal(0, definition.SelectFrame(FrameKind.Idle, float.NaN).Index);
        Assert.Equal(6, definition.SelectFrame(FrameKind.Jump, 999).Index);
        Assert.Equal(7, definition.SelectFrame(FrameKind.Apex, 999).Index);
        Assert.Equal(8, definition.SelectFrame(FrameKind.Fall, 999).Index);
        var left = definition.SelectFrame(FrameKind.WalkLeft, .251f);
        Assert.Equal(5, left.Index);
        Assert.True(left.Mirrored);
        var sequence = new[] { 3, 4, 5, 4 };
        var immutable = new CharacterAnimation(3, 8) { WalkFrames = sequence };
        sequence[0] = 5;
        Assert.Equal(3, immutable.WalkFrames[0]);
    }

    [Theory]
    [InlineData("{\"animation\":{\"walkFrames\":[]}}")]
    [InlineData("{\"animation\":{\"walkFrames\":[2]}}")]
    [InlineData("{\"animation\":{\"idleFrames\":[3]}}")]
    [InlineData("{\"animation\":{\"walkFrames\":[3.5]}}")]
    [InlineData("{\"animation\":{\"walkFrames\":[\"3\"]}}")]
    [InlineData("{\"animation\":{\"walkStridePixels\":0}}")]
    [InlineData("{\"rendering\":{\"sampling\":\"motion-blur\"}}")]
    public void InvalidPlaybackAndSamplingAreRejected(string json)
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), json);
            Assert.Throws<CharacterLoadException>(() => CharacterLoader.LoadDirectory(directory));
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    [Fact]
    public void EnteringNewStateResetsAnimationButContinuingWalkKeepsItsPhase()
    {
        using var definition = Configured();
        var pet = new PetInstance(definition, new(200, 900)) { AnimationTime = .26f };
        pet.StartWalking(FacingDirection.Right);
        Assert.Equal(0, pet.AnimationTime);
        var engine = Engine();
        for (var i = 0; i < 10; i++) engine.Tick(pet, .05);
        Assert.True(pet.AnimationTime > 0);
        engine.BeginDrag(pet);
        Assert.Equal(0, pet.AnimationTime);
        engine.ReleaseDrag(pet, new Vector2(0, -400));
        Assert.Equal(0, pet.AnimationTime);
        pet.State = PetState.Idle;
        pet.AnimationTime = 123;
        Assert.True(pet.Feed());
        Assert.Equal(0, pet.AnimationTime);
    }

    [Theory]
    [InlineData(1, .016)]
    [InlineData(2, .033)]
    [InlineData(.5, .07)]
    public void WalkingCycleTracksDistanceAndEffectiveScale(float scale, double dt)
    {
        using var definition = Configured();
        var pet = new PetInstance(definition, new(200, 900));
        pet.StartWalking(FacingDirection.Right);
        var engine = Engine();
        engine.GlobalScale = scale;
        for (var i = 0; i < 25; i++) engine.Tick(pet, dt);
        var distance = pet.FootPosition.X - 200;
        var cycles = pet.AnimationTime * definition.Animation.WalkFps / definition.Animation.WalkFrames.Count;
        Assert.Equal(distance / (100 * scale), cycles, 3);
        Assert.Equal(PetState.Walk, pet.State);
    }

    [Theory]
    [InlineData(.016)]
    [InlineData(.033)]
    [InlineData(.07)]
    [InlineData(.1)]
    public void JumpCannotSkipApexAndItsPoseDoesNotFreezePhysics(double dt)
    {
        using var definition = Configured();
        var pet = new PetInstance(definition, new(200, 900));
        var engine = Engine();
        pet.StartJump();
        var frames = new List<int> { 6 };
        var sawApexMove = false;
        for (var i = 0; i < 300 && pet.State != PetState.Idle; i++)
        {
            var previous = pet.State;
            var y = pet.FootPosition.Y;
            engine.Tick(pet, dt);
            frames.Add(definition.SelectFrame(pet.GetFrameKind(), pet.AnimationTime).Index);
            sawApexMove |= previous == PetState.Apex && pet.FootPosition.Y > y;
        }
        Assert.Contains(7, frames);
        Assert.Contains(8, frames);
        Assert.Equal(PetState.Idle, pet.State);
        Assert.Equal(900, pet.FootPosition.Y);
        Assert.True(sawApexMove);
    }

    [Fact]
    public void DefaultSpriteOnlyPlaybackRemainsThreeFrames()
    {
        using var definition = TestSpriteFactory.LoadSpriteOnly("legacy");
        Assert.Equal(new[] { 3, 4, 5, 3 }, Enumerable.Range(0, 4)
            .Select(i => definition.SelectFrame(FrameKind.WalkRight, i / 8f + .001f).Index));
        Assert.Equal(SpriteSampling.Nearest, definition.Sampling);
        Assert.Null(definition.Animation.WalkStridePixels);
    }

    [Fact]
    public void WrappingDoesNotAdvanceAnEntireDesktopOfWalkFrames()
    {
        using var definition = Configured();
        var pet = new PetInstance(definition, new(3998, 900));
        pet.StartWalking(FacingDirection.Right);
        var engine = Engine();
        engine.Tick(pet, .1);
        Assert.Equal(4, pet.FootPosition.X, 3);
        Assert.Equal(.03f, pet.AnimationTime, 4); // six actual pixels, not 3994
    }

    [Theory]
    [InlineData("咿米_Yimy", "63CE5BF9750B0ADF796A0CBB05616A4731003C01CFC23819FFD0DB435B66CA23")]
    [InlineData("咿米_Line", "EDB50E55FAD02497ADB73E9FE1509A30C9FA08EA1F7E756448B4A00644EA894E")]
    public void OwnerSuppliedYimyArtworkRemainsByteIdenticalWithExplicitPlaybackProfile(string role, string sha256)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "YimyWalk.sln")))
            root = root.Parent ?? throw new DirectoryNotFoundException("repository root");
        var directory = Path.Combine(root.FullName, "Characters", role);
        Assert.Equal(sha256, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "sprite.png")))));
        using var definition = CharacterLoader.LoadDirectory(directory);
        Assert.Equal(9, definition.Frames.Count);
        Assert.Equal(new Anchor(209, 418), definition.Anchor);
        Assert.Equal(new[] { 0 }, definition.Animation.IdleFrames);
        Assert.Equal(new[] { 3, 4, 5, 4 }, definition.Animation.WalkFrames);
        Assert.Equal(SpriteSampling.Smooth, definition.Sampling);
    }

    private static CharacterDefinition Configured()
    {
        var directory = TestSpriteFactory.WriteSprite(96, 96, false);
        try
        {
            File.WriteAllText(Path.Combine(directory, "character.json"), """
                {"animation":{"idleFrames":[0],"walkFrames":[3,4,5,4],"walkStridePixels":100},"rendering":{"sampling":"smooth"}}
                """);
            return CharacterLoader.LoadDirectory(directory);
        }
        finally { TestSpriteFactory.DeleteFixture(directory); }
    }

    private static PetEngine Engine() => new(new Surface(), new Random()) { JumpDesire = 0, WindowJumpDesire = 0, ClimbDesire = 0 };
    private sealed class Surface : IWorldSurface { public WorldRect WorkingArea => new(0, 0, 4000, 900); public bool IsValid => true; }
    private sealed class Random : IRandomSource { public double NextDouble() => .5; public int Next(int minInclusive, int maxExclusive) => minInclusive; }
}
