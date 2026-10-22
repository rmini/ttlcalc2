namespace TtlCalcSim.Testing;

/// <summary>
/// Locates files in the TtlCalc2 repository relative to the running test assembly.
/// </summary>
public static class RepoPaths
{
    private static readonly Lazy<string> RootLazy = new(FindRoot);

    /// <summary>Repository root (the directory containing <c>sw/ttlcalc2.asm</c>).</summary>
    public static string Root => RootLazy.Value;

    /// <summary>The <c>sw</c> directory containing assembly sources.</summary>
    public static string Sw => Path.Combine(Root, "sw");

    /// <summary>The customasm rule definitions for the TtlCalc2 instruction set.</summary>
    public static string Rules => Path.Combine(Sw, "ttlcalc2.asm");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "sw", "ttlcalc2.asm")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException(
            $"Could not find sw/ttlcalc2.asm in any parent directory of {AppContext.BaseDirectory}");
    }
}
