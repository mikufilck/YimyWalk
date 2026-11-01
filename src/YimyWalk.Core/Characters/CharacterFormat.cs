namespace YimyWalk.Core.Characters;

public enum FrameKind
{
    Idle,
    WalkRight,
    WalkLeft,
    Jump,
    Apex,
    Fall
}

public readonly record struct Anchor(float X, float Y);

public sealed record CharacterFrame(
    int Index,
    int Width,
    int Height,
    ReadOnlyMemory<byte> RgbaPixels,
    FrameKind Kind,
    bool Mirrored = false);

public sealed record CharacterMovement(
    float WalkSpeed,
    float JumpSpeed,
    float Gravity,
    float MaxFallSpeed);

public sealed record CharacterDefaults(
    float IdleSecondsMin,
    float IdleSecondsMax,
    float SpeechCooldownSeconds,
    float HungerIncreasePerMinute = 0.01f,
    float WalkSecondsMin = 3f,
    float WalkSecondsMax = 20f);

public sealed record CharacterAnimation(float IdleFps, float WalkFps)
{
    private static readonly IReadOnlyList<int> DefaultIdle = Array.AsReadOnly(new[] { 0, 1, 2 });
    private static readonly IReadOnlyList<int> DefaultWalk = Array.AsReadOnly(new[] { 3, 4, 5 });
    private IReadOnlyList<int> _idleFrames = DefaultIdle;
    private IReadOnlyList<int> _walkFrames = DefaultWalk;
    public IReadOnlyList<int> IdleFrames { get => _idleFrames; init => _idleFrames = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<int> WalkFrames { get => _walkFrames; init => _walkFrames = Array.AsReadOnly(value.ToArray()); }
    // Optional source-image pixels travelled per complete walking cycle.
    // Null preserves the time-based animation contract of sprite-only roles.
    public float? WalkStridePixels { get; init; }
}

public enum SpriteSampling { Nearest, Smooth }

public sealed class CharacterLoadException : Exception
{
    public CharacterLoadException(string directory, string reason, Exception? innerException = null)
        : base($"Could not load character '{directory}': {reason}", innerException)
    {
        DirectoryPath = directory;
    }

    public string DirectoryPath { get; }
}
