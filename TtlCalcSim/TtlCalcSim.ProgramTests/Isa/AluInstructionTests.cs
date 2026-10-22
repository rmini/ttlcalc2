using TtlCalcSim.Testing;
using static TtlCalcSim.ProgramTests.Isa.IsaReference;

namespace TtlCalcSim.ProgramTests.Isa;

/// <summary>
/// End-to-end ALU instruction tests: each instruction is assembled with customasm, run in the
/// simulator for every input combination, and compared against <see cref="IsaReference"/>.
/// </summary>
public class AluInstructionTests
{
    /// <summary>Auxiliary flags, which ALU operations are assumed to leave unchanged.</summary>
    private const Flags Aux = Flags.XF | Flags.YF;

    private static readonly int[] CarryIn = [0, 1];

    [Theory]
    [InlineData("add")]
    [InlineData("addc")]
    [InlineData("sub")]
    [InlineData("subc")]
    public void Arithmetic_MatchesReference(string mnemonic)
    {
        var program = AsmProgram.FromSource($"{mnemonic} l, x, y");
        var useCarry = mnemonic.EndsWith('c');
        var mismatches = new Mismatches(mnemonic);

        foreach (var (x, y, cf) in Inputs(16, 16))
        {
            var m = Run(program, x, y, cf);
            var c = useCarry ? cf : 0;
            var expected = mnemonic.StartsWith("add") ? Add(x, y, c) : Sub(x, y, c);
            var ctx = $"X={x:X} Y={y:X} CF={cf}";
            mismatches.Expect(expected.Out, (int)m.L, $"{ctx} result");
            mismatches.Expect(expected.Carry, m.Flags.HasFlag(Flags.CF), $"{ctx} CF");
            mismatches.Expect(expected.Equal, m.Flags.HasFlag(Flags.EF), $"{ctx} EF");
            mismatches.Expect(Aux, m.Flags & Aux, $"{ctx} aux flags");
        }

        mismatches.AssertNone();
    }

    [Theory]
    [InlineData("addd")]
    [InlineData("addcd")]
    [InlineData("subd")]
    [InlineData("subcd")]
    public void Bcd_MatchesReferenceForDecimalDigits(string mnemonic)
    {
        var program = AsmProgram.FromSource($"{mnemonic} l, x, y");
        var useCarry = mnemonic.Contains("cd");
        var mismatches = new Mismatches(mnemonic);

        foreach (var (x, y, cf) in Inputs(10, 10))
        {
            var m = Run(program, x, y, cf);
            var c = useCarry ? cf : 0;
            var expected = mnemonic.StartsWith("add") ? AddBcd(x, y, c) : SubBcd(x, y, c);
            var ctx = $"X={x} Y={y} CF={cf}";
            mismatches.Expect(expected.Out, (int)m.L, $"{ctx} result");
            mismatches.Expect(expected.Carry, m.Flags.HasFlag(Flags.CF), $"{ctx} CF");
            mismatches.Expect(Aux, m.Flags & Aux, $"{ctx} aux flags");
        }

        mismatches.AssertNone();
    }

    public static TheoryData<string> LogicOps => new() { "or", "and", "xor", "nor", "nand", "xnor" };

    [Theory]
    [MemberData(nameof(LogicOps))]
    public void Logic_MatchesReference(string mnemonic)
    {
        Func<int, int, int> op = mnemonic switch
        {
            "or" => Or,
            "and" => And,
            "xor" => Xor,
            "nor" => Nor,
            "nand" => Nand,
            "xnor" => Xnor,
            _ => throw new ArgumentOutOfRangeException(nameof(mnemonic)),
        };
        var program = AsmProgram.FromSource($"{mnemonic} l, x, y");
        var mismatches = new Mismatches(mnemonic);

        foreach (var (x, y, cf) in Inputs(16, 16))
        {
            var m = Run(program, x, y, cf);
            var expected = op(x, y);
            var ctx = $"X={x:X} Y={y:X} CF={cf}";
            mismatches.Expect(expected, (int)m.L, $"{ctx} result");
            mismatches.Expect(expected == 0xF, m.Flags.HasFlag(Flags.EF), $"{ctx} EF");
            mismatches.Expect(Aux, m.Flags & Aux, $"{ctx} aux flags");
        }

        mismatches.AssertNone();
    }

    [Theory]
    [InlineData("shl l, x")]
    [InlineData("dec l, x")]
    [InlineData("not l, x")]
    [InlineData("mov l, y")]
    [InlineData("not l, y")]
    public void Unary_MatchesReference(string source)
    {
        var program = AsmProgram.FromSource(source);
        var mismatches = new Mismatches(source);

        foreach (var (x, y, cf) in Inputs(16, 16))
        {
            var m = Run(program, x, y, cf);
            var ctx = $"X={x:X} Y={y:X} CF={cf}";
            switch (source)
            {
                case "shl l, x" or "dec l, x":
                    var expected = source.StartsWith("shl") ? Shl(x, 0) : Dec(x, 0);
                    mismatches.Expect(expected.Out, (int)m.L, $"{ctx} result");
                    mismatches.Expect(expected.Carry, m.Flags.HasFlag(Flags.CF), $"{ctx} CF");
                    mismatches.Expect(expected.Equal, m.Flags.HasFlag(Flags.EF), $"{ctx} EF");
                    break;
                default:
                    var logic = source switch
                    {
                        "not l, x" => NotX(x),
                        "mov l, y" => MovY(y),
                        _ => NotY(y),
                    };
                    mismatches.Expect(logic, (int)m.L, $"{ctx} result");
                    mismatches.Expect(logic == 0xF, m.Flags.HasFlag(Flags.EF), $"{ctx} EF");
                    break;
            }

            mismatches.Expect(Aux, m.Flags & Aux, $"{ctx} aux flags");
        }

        mismatches.AssertNone();
    }

    [Fact]
    public void FlagsDestination_TakesAluFlagsNotResult()
    {
        // 9 + 9 = 0x12: result 2 (which would read as EF), carry out. instr.md: on ALU
        // operations the ALU output has priority over the data bus for setting the flags register.
        var m = Run(AsmProgram.FromSource("add Flags, x, y"), 9, 9, 0);

        Assert.Equal(Flags.CF | Aux, m.Flags);
    }

    [Fact]
    public void NoneDestination_SetsFlagsOnly()
    {
        var m = Run(AsmProgram.FromSource("sub None, x, y"), 5, 5, 0);

        Assert.Equal(Flags.EF | Aux, m.Flags);
        Assert.Equal([5, 5, 0, 0], new[] { m.X, m.Y, m.H, m.L });
        Assert.Empty(m.IoWrites);
    }

    [Theory]
    [InlineData("add Mem[hl], x, y", 0x37)]
    [InlineData("add Mem[hl++], x, y", 0x38)]
    [InlineData("add(hl++) Mem[hl], x, y", 0x38)]
    [InlineData("addd(hl--) Mem[hl], x, y", 0x36)]
    [InlineData("addd Mem[hl--], x, y", 0x36)]
    public void MemoryDestination_StoresAtHlThenUpdatesHl(string source, int expectedHl)
    {
        var m = new TestMachine(AsmProgram.FromSource(source)) { X = 2, Y = 3, HL = 0x37 };

        m.RunToEnd();

        Assert.Equal(5, m.Mem[0x37]);
        Assert.Equal(expectedHl, m.HL);
    }

    [Theory]
    [InlineData("add Mem[#9], x, y", 9)]
    [InlineData("or Mem[#14], x, y", 14)]
    public void ZeroPageDestination_UsesOperationCodeAsAddress(string source, int addr)
    {
        var m = new TestMachine(AsmProgram.FromSource(source)) { X = 1, Y = 4, HL = 0x80 };

        m.RunToEnd();

        Assert.Equal(5, m.Mem[addr]);
        Assert.Equal(0x80, m.HL);
    }

    [Fact]
    public void IoDestination_WritesResult()
    {
        var m = new TestMachine(AsmProgram.FromSource("add IO[hl], x, y")) { X = 6, Y = 7, HL = 0x02 };

        m.RunToEnd();

        Assert.Equal([new IoWrite(1, 0x02, 0xD)], m.IoWrites);
    }

    private static IEnumerable<(int x, int y, int cf)> Inputs(int xCount, int yCount) =>
        from x in Enumerable.Range(0, xCount)
        from y in Enumerable.Range(0, yCount)
        from cf in CarryIn
        select (x, y, cf);

    private static TestMachine Run(AsmProgram program, int x, int y, int cf)
    {
        var m = new TestMachine(program, traceLength: 0)
        {
            X = (byte)x,
            Y = (byte)y,
            Flags = Aux | (cf == 1 ? Flags.CF : Flags.None),
        };
        m.RunToEnd(maxSteps: 10);
        return m;
    }
}
