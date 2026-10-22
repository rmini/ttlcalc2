namespace TtlCalcSim.Testing;

/// <summary>
/// An assembled TtlCalc2 program: its instruction words and symbol table.
/// </summary>
public sealed class AsmProgram
{
    internal AsmProgram(string name, ushort[] words, IReadOnlyDictionary<string, int> symbols)
    {
        Name = name;
        Words = words;

        // customasm fills unused program space with zeros. A trailing all-zero word is
        // indistinguishable from fill (it is also NOP), so the program ends after the
        // last non-zero word.
        var end = words.Length;
        while (end > 0 && words[end - 1] == 0)
            end--;
        End = end;

        Symbols = symbols;
    }

    /// <summary>Source file name, or a description for programs assembled from source text.</summary>
    public string Name { get; }

    /// <summary>All program words, including fill.</summary>
    public IReadOnlyList<ushort> Words { get; }

    /// <summary>Address one past the last instruction.</summary>
    public int End { get; }

    /// <summary>
    /// All symbols from the program. Data and program labels share this table; local labels
    /// are qualified with their parent (e.g. <c>digloop.digsdone</c>).
    /// </summary>
    public IReadOnlyDictionary<string, int> Symbols { get; }

    /// <summary>Gets the value of a symbol.</summary>
    public int this[string symbol] =>
        Symbols.TryGetValue(symbol, out var value)
            ? value
            : throw new KeyNotFoundException(
                $"Symbol '{symbol}' not found in {Name}. Known symbols: {string.Join(", ", Symbols.Keys)}");

    /// <summary>Assembles a source file with customasm.</summary>
    public static AsmProgram Assemble(string path) => Assembler.AssembleFile(path);

    /// <summary>Assembles a file from the repository's <c>sw</c> directory.</summary>
    public static AsmProgram AssembleSw(string fileName) => Assembler.AssembleFile(Path.Combine(RepoPaths.Sw, fileName));

    /// <summary>
    /// Assembles source text with the TtlCalc2 rules included and the program bank selected.
    /// </summary>
    public static AsmProgram FromSource(string source) => Assembler.AssembleSource(source);
}
