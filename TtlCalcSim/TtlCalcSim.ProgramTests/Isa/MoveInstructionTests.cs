using TtlCalcSim.Testing;

namespace TtlCalcSim.ProgramTests.Isa;

/// <summary>
/// End-to-end data movement tests: sources, destinations, addressing modes, and HL
/// post-increment/decrement, assembled with customasm and run in the simulator.
/// </summary>
public class MoveInstructionTests
{
    [Theory]
    [InlineData("x")]
    [InlineData("y")]
    [InlineData("h")]
    [InlineData("l")]
    public void Immediate_ToRegister(string reg)
    {
        for (var v = 0; v < 16; v++)
        {
            var m = RunSource($"mov {reg}, #{v}");

            Assert.Equal(v, Register(m, reg));
        }
    }

    [Theory]
    [InlineData("x", "h")]
    [InlineData("x", "l")]
    [InlineData("y", "x")]
    [InlineData("h", "x")]
    [InlineData("l", "h")]
    public void Register_ToRegister(string dst, string src)
    {
        var m = new TestMachine(AsmProgram.FromSource($"mov {dst}, {src}")) { X = 0x1, H = 0x2, L = 0x3 };
        var expected = Register(m, src);

        m.RunToEnd();

        Assert.Equal(expected, Register(m, dst));
    }

    [Fact]
    public void Flags_ImmediateSetsAllBitsAndRegisterReadsThemBack()
    {
        for (var v = 0; v < 16; v++)
        {
            var m = RunSource($"mov Flags, #{v}\nmov x, Flags");

            Assert.Equal((Flags)v, m.Flags);
            Assert.Equal(v, m.X);
        }
    }

    [Fact]
    public void ZeroPageMemory_WriteAndRead_IgnoreHl()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov Mem[#5], x
                mov y, Mem[#5]
                mov h, Mem[#5]
            """)) { X = 0xA, HL = 0x55 };

        m.RunToEnd();

        Assert.Equal(0xA, m.Mem[5]);
        Assert.Equal(0, m.Mem[0x55]);
        Assert.Equal(0xA, m.Y);
        Assert.Equal(0xA, m.H);
        Assert.Equal(5, m.L);
    }

    [Fact]
    public void HlMemory_WriteAndRead_UseHl()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov Mem[hl], x
                mov y, Mem[hl]
            """)) { X = 0x6, HL = 0xA7 };

        m.RunToEnd();

        Assert.Equal(0x6, m.Mem[0xA7]);
        Assert.Equal(0x6, m.Y);
        Assert.Equal(0xA7, m.HL);
    }

    [Fact]
    public void ImmediateToZeroPageMemory_WhenAddressEqualsValue()
    {
        var m = RunSource("mov Mem[#3], #3");

        Assert.Equal(3, m.Mem[3]);
    }

    [Fact]
    public void Io_ZeroPageAndHl_WriteAndRead()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov IO[#3], x
                mov IO[hl], l
                mov y, IO[#4]
                mov x, IO[hl]
            """)) { X = 0x9, HL = 0x2C };
        m.QueueInput(4, 0x5);
        m.QueueInput(0x2C, 0x7);

        m.RunToEnd();

        Assert.Equal([new IoWrite(1, 3, 0x9), new IoWrite(2, 0x2C, 0xC)], m.IoWrites);
        Assert.Equal(0x5, m.Y);
        Assert.Equal(0x7, m.X);
    }

    [Theory]
    [InlineData("mov(hl++) x, #1", 0x0F, 0x10)]
    [InlineData("mov(hl++) x, #1", 0xFF, 0x00)]
    [InlineData("mov(hl--) x, #1", 0x10, 0x0F)]
    [InlineData("mov(hl--) x, #1", 0x00, 0xFF)]
    [InlineData("nop(hl++)", 0x3F, 0x40)]
    [InlineData("nop(hl--)", 0x40, 0x3F)]
    [InlineData("mov x, Mem[hl++]", 0x3F, 0x40)]
    [InlineData("mov x, Mem[hl--]", 0x3F, 0x3E)]
    [InlineData("mov Mem[hl++], x", 0xEF, 0xF0)]
    [InlineData("mov IO[hl--], x", 0x01, 0x00)]
    [InlineData("mov x, Mem[hl]", 0x3F, 0x3F)]
    [InlineData("mov x, #1", 0x3F, 0x3F)]
    public void Hl_PostIncrementDecrement(string source, int startHl, int expectedHl)
    {
        var m = new TestMachine(AsmProgram.FromSource(source)) { HL = (byte)startHl };

        m.RunToEnd();

        Assert.Equal(expectedHl, m.HL);
    }

    [Fact]
    public void Hl_PostIncrement_AccessesOriginalAddress()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov Mem[hl++], x
                mov y, Mem[hl--]
            """)) { X = 0x4, HL = 0x20 };
        m.Mem[0x21] = 0x8;

        m.RunToEnd();

        Assert.Equal(0x4, m.Mem[0x20]);
        Assert.Equal(0x8, m.Y);
        Assert.Equal(0x20, m.HL);
    }

    [Fact]
    public void Nop_And_MoveToNone_ChangeNothing()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                nop
                mov None, x
                mov None, Mem[hl]
                mov None, #7
            """)) { X = 1, Y = 2, HL = 0x34, Flags = Flags.XF };

        m.RunToEnd();

        Assert.Equal([1, 2, 3, 4], new[] { m.X, m.Y, m.H, m.L });
        Assert.Equal(Flags.XF, m.Flags);
        Assert.All(Enumerable.Range(0, 256), a => Assert.Equal(0, m.Mem[a]));
        Assert.Empty(m.IoWrites);
    }

    private static TestMachine RunSource(string source)
    {
        var m = new TestMachine(AsmProgram.FromSource(source));
        m.RunToEnd();
        return m;
    }

    private static byte Register(TestMachine m, string reg) => reg switch
    {
        "x" => m.X,
        "y" => m.Y,
        "h" => m.H,
        "l" => m.L,
        _ => throw new ArgumentOutOfRangeException(nameof(reg)),
    };
}
