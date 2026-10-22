namespace TtlCalcSim.ProgramTests.Isa;

/// <summary>
/// Collects mismatches across many input combinations so one test reports all of them.
/// </summary>
public sealed class Mismatches(string what)
{
    private const int MaxReported = 32;

    private readonly List<string> _items = [];

    public void Expect<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            _items.Add($"{context}: expected {expected}, got {actual}");
    }

    public void AssertNone()
    {
        if (_items.Count == 0)
            return;

        var shown = string.Join("\n  ", _items.Take(MaxReported));
        var more = _items.Count > MaxReported ? $"\n  ... and {_items.Count - MaxReported} more" : "";
        Assert.Fail($"{what}: {_items.Count} mismatch(es)\n  {shown}{more}");
    }
}
