namespace TtlCalcSim.ProgramTests.Isa;

/// <summary>
/// Expected instruction behavior, written from doc/instr.md, doc/arch.md, and doc/impl.md
/// independently of the simulator's implementation.
/// </summary>
public static class IsaReference
{
    /// <summary>Result of an ALU operation: 4-bit output, carry flag, and equal (all ones) flag.</summary>
    public readonly record struct AluResult(int Out, bool Carry, bool Equal);

    private static AluResult Result(int value, bool carry)
    {
        var o = value & 0xF;
        return new AluResult(o, carry, o == 0xF);
    }

    // Arithmetic mode. Carry out is the 74181 carry: set when the sum exceeds 15, which for
    // subtraction means "no borrow" (impl.md: no carry out indicates Y is larger than X).

    /// <summary>ADD: X + Y + C</summary>
    public static AluResult Add(int x, int y, int c) => Result(x + y + c, x + y + c > 15);

    /// <summary>SUB: X - Y - 1 + C</summary>
    public static AluResult Sub(int x, int y, int c) => Result(x - y - 1 + c, x - y - 1 + c >= 0);

    /// <summary>SHL: X + X + C</summary>
    public static AluResult Shl(int x, int c) => Add(x, x, c);

    /// <summary>DEC: X - 1 + C</summary>
    public static AluResult Dec(int x, int c) => Result(x - 1 + c, x - 1 + c >= 0);

    // BCD mode (impl.md). For valid BCD digits the hardware's +6/+10 corrections produce the
    // decimal result, with carry meaning decimal carry (add) or no decimal borrow (sub). The
    // equal flag is not specified for BCD results, so it is not modeled here.

    /// <summary>BCD ADD: X + Y + C, decimal</summary>
    public static (int Out, bool Carry) AddBcd(int x, int y, int c) => ((x + y + c) % 10, x + y + c >= 10);

    /// <summary>BCD SUB: X - Y - 1 + C, decimal</summary>
    public static (int Out, bool Carry) SubBcd(int x, int y, int c)
    {
        var d = x - y - 1 + c;
        return ((d + 10) % 10, d >= 0);
    }

    // Logic mode. Carry out is not specified for logic operations, so only the output and
    // equal flag are modeled.

    public static int Or(int x, int y) => (x | y) & 0xF;
    public static int And(int x, int y) => x & y & 0xF;
    public static int Xor(int x, int y) => (x ^ y) & 0xF;
    public static int Nor(int x, int y) => ~(x | y) & 0xF;
    public static int Nand(int x, int y) => ~(x & y) & 0xF;
    public static int Xnor(int x, int y) => ~(x ^ y) & 0xF;
    public static int NotX(int x) => ~x & 0xF;
    public static int NotY(int y) => ~y & 0xF;
    public static int MovY(int y) => y & 0xF;

    /// <summary>Branch conditions (instr.md, "Condition codes").</summary>
    public static bool BranchTaken(string cond, Flags flags)
    {
        var cf = flags.HasFlag(Flags.CF);
        var ef = flags.HasFlag(Flags.EF);
        return cond switch
        {
            "z" or "e" => ef,
            "nz" or "ne" => !ef,
            "c" or "lt" => cf,
            "nc" or "ge" => !cf,
            "gt" => !cf && !ef,
            "le" => cf || ef,
            "mp" => true,
            _ => throw new ArgumentOutOfRangeException(nameof(cond), cond, null),
        };
    }

    /// <summary>All jump mnemonic suffixes (j{cond}).</summary>
    public static readonly string[] Conditions = ["z", "e", "nz", "ne", "c", "lt", "nc", "ge", "gt", "le", "mp"];
}
