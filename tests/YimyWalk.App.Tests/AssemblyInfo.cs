using Xunit;

// WinForms and GDI use process-wide caches; resource assertions run serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
