namespace TtlCalcSim.Testing;

/// <summary>One executed instruction in a <see cref="TestMachine"/> trace.</summary>
public readonly record struct TraceEntry(long Step, int PC, Operation Op)
{
    public override string ToString() => $"{Step,8}  {PC:X3}: {Op.ToUInt16():X4}  {Op.Disassemble()}";
}

/// <summary>
/// Thrown when a program run does not stop where expected: it exceeded its step limit or ran
/// past the end of the program. The message includes the most recently executed instructions.
/// </summary>
public class ProgramRunException(string message, IReadOnlyList<TraceEntry> trace)
    : Exception(message + FormatTrace(trace))
{
    public IReadOnlyList<TraceEntry> Trace { get; } = trace;

    private static string FormatTrace(IReadOnlyList<TraceEntry> trace) =>
        trace.Count == 0
            ? ""
            : "\nLast instructions executed:\n" + string.Join("\n", trace.Select(t => "  " + t));
}
