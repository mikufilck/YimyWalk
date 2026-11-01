using System.Diagnostics;

namespace YimyWalk.App.Host;

internal interface IMonotonicClock { double Seconds { get; } }

internal sealed class StopwatchClock : IMonotonicClock
{
    public static StopwatchClock Instance { get; } = new();
    public double Seconds => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
}

internal interface IAppLog { void Record(string code, Exception? error = null); }

/// <summary>Bounded local diagnostics: stable operation codes and error types only.</summary>
internal sealed class AppLog(string directory, IMonotonicClock clock) : IAppLog
{
    internal const long MaxFileBytes = 1024 * 1024;
    private readonly Dictionary<string, double> _lastWrite = new();
    public void Record(string code, Exception? error = null)
    {
        var key = code + ":" + (error?.GetType().Name ?? "OK");
        if (_lastWrite.TryGetValue(key, out var last) && clock.Seconds - last < 60) return;
        _lastWrite[key] = clock.Seconds;
        try
        {
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "runtime.log");
            if (File.Exists(path) && new FileInfo(path).Length >= MaxFileBytes)
                File.Move(path, path + ".1", overwrite: true);
            File.AppendAllText(path, $"{clock.Seconds:F3} {key}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}

internal sealed class NullAppLog : IAppLog
{
    public static NullAppLog Instance { get; } = new();
    public void Record(string code, Exception? error = null) { }
}
