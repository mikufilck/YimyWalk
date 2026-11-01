using System.Collections.ObjectModel;

namespace YimyWalk.Core.Characters;

using YimyWalk.Core.Resources;

public sealed record CharacterScanFailure(string DirectoryPath, string Reason);

public sealed class CharacterCatalog : IDisposable
{
    private IReadOnlyDictionary<string, CharacterDefinition> _definitions =
        new ReadOnlyDictionary<string, CharacterDefinition>(new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase));
    private Dictionary<string, DefinitionLease> _leases = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;
    private readonly Func<string, CharacterDefinition> _load;
    private readonly Func<string, string[]> _enumerate;

    public CharacterCatalog() : this(CharacterLoader.LoadDirectory, Directory.GetDirectories) { }

    internal CharacterCatalog(Func<string, CharacterDefinition> load, Func<string, string[]> enumerate)
    {
        _load = load;
        _enumerate = enumerate;
    }

    public IReadOnlyList<CharacterScanFailure> LastFailures { get; private set; } = Array.Empty<CharacterScanFailure>();
    public IReadOnlyDictionary<string, CharacterDefinition> Definitions => _definitions;

    public bool TryGet(string id, out CharacterDefinition definition)
        => _definitions.TryGetValue(id, out definition!);

    public IReadOnlyDictionary<string, CharacterDefinition> Scan(string root)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Directory.Exists(root))
        {
            LastFailures = new[] { new CharacterScanFailure(root, "root directory does not exist") };
            return _definitions;
        }

        string[] directories;
        try { directories = _enumerate(root); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            LastFailures = new[] { new CharacterScanFailure(root, ex.Message) };
            return _definitions;
        }
        var next = new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase);
        var nextLeases = new Dictionary<string, DefinitionLease>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<CharacterScanFailure>();
        var committed = false;
        try
        {
            foreach (var directory in directories)
            {
                try
                {
                    var definition = _load(directory);
                    var lease = new DefinitionLease(definition);
                    if (nextLeases.TryGetValue(definition.Id, out var duplicate) && !_leases.Values.Contains(duplicate)) duplicate.Dispose();
                    next[definition.Id] = definition;
                    nextLeases[definition.Id] = lease;
                }
                catch (Exception ex) when (ex is CharacterLoadException or IOException or UnauthorizedAccessException or System.Security.SecurityException)
                {
                    failures.Add(new CharacterScanFailure(directory, ex.Message));
                    if (_definitions.TryGetValue(Path.GetFileName(directory), out var previous))
                    {
                        next[previous.Id] = previous;
                        if (_leases.TryGetValue(previous.Id, out var previousLease)) nextLeases[previous.Id] = previousLease;
                    }
                }
            }

            var oldLeases = _leases;
            _definitions = new ReadOnlyDictionary<string, CharacterDefinition>(next);
            _leases = nextLeases;
            committed = true;
            LastFailures = failures;
            foreach (var lease in oldLeases.Values)
                if (!nextLeases.Values.Contains(lease)) lease.Dispose();
            return _definitions;
        }
        finally
        {
            if (!committed)
                foreach (var lease in nextLeases.Values)
                    if (!_leases.Values.Contains(lease)) lease.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var lease in _leases.Values) lease.Dispose();
        _definitions = new ReadOnlyDictionary<string, CharacterDefinition>(new Dictionary<string, CharacterDefinition>(StringComparer.OrdinalIgnoreCase));
        _leases = new Dictionary<string, DefinitionLease>(StringComparer.OrdinalIgnoreCase);
    }
}
