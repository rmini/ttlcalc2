using System.Numerics;
using TtlCalcSim.Testing;

namespace TtlCalcSim.ProgramTests;

public class FibTests
{
    private const int Digits = 8;
    private const int FirstNumber = 0x10;
    private const int NumberCount = (0x100 - FirstNumber) / Digits;

    private readonly TestMachine _m = new(AsmProgram.AssembleSw("fib.asm"));

    [Fact]
    public void Run_FinishesWithinStepLimit()
    {
        var steps = _m.RunToEnd(maxSteps: 10_000);

        Assert.InRange(steps, 1, 10_000);
    }

    [Fact]
    public void Run_ComputesFibonacciNumbersInBcd()
    {
        _m.RunToEnd(maxSteps: 10_000);

        BigInteger a = 1, b = 1;
        for (var i = 0; i < NumberCount; i++)
        {
            var addr = FirstNumber + i * Digits;
            Assert.True((long)(a % 100_000_000) == _m.ReadNumber(addr, Digits),
                $"Number {i} at 0x{addr:X2}: expected {a % 100_000_000}, got {_m.ReadNumber(addr, Digits)}");
            (a, b) = (b, a + b);
        }
    }

    [Fact]
    public void Startup_TurnsOnBusyLightAndClearsDisplayControls()
    {
        _m.RunUntil("numloop");

        Assert.Equal(
            [(3, 1), (0, 0xF), (1, 0xF), (2, 0xF)],
            _m.IoWrites.Select(w => ((int)w.Addr, (int)w.Value)));
    }

    [Fact]
    public void Startup_InitializesFirstNumbersAndPointers()
    {
        _m.RunUntil("numloop");

        Assert.Equal(1, _m.ReadNumber(0x10, Digits));
        Assert.Equal(1, _m.ReadNumber(0x18, Digits));
        Assert.Equal(0x10, Pointer("src1"));
        Assert.Equal(0x18, Pointer("src2"));
        Assert.Equal(0x20, Pointer("dst"));
    }

    [Theory]
    [InlineData(12345678, 87654321, 99999999)]
    [InlineData(19999999, 1, 20000000)]
    [InlineData(99999999, 1, 0)]
    [InlineData(55555555, 55555555, 11111110)]
    public void NumLoop_AddsSourcesIntoDestinationAndAdvancesPointers(long src1, long src2, long expected)
    {
        SetPointer("src1", 0x40);
        SetPointer("src2", 0x48);
        SetPointer("dst", 0x50);
        _m.WriteNumber(0x40, Digits, src1);
        _m.WriteNumber(0x48, Digits, src2);

        _m.StartAt("numloop").RunUntil("numloop", maxSteps: 1_000);

        Assert.Equal(expected, _m.ReadNumber(0x50, Digits));
        Assert.Equal(src1, _m.ReadNumber(0x40, Digits));
        Assert.Equal(src2, _m.ReadNumber(0x48, Digits));
        Assert.Equal(0x48, Pointer("src1"));
        Assert.Equal(0x50, Pointer("src2"));
        Assert.Equal(0x58, Pointer("dst"));
    }

    private int Pointer(string name) => _m.Mem[name + "h"] << 4 | _m.Mem[name + "l"];

    private void SetPointer(string name, int addr)
    {
        _m.Mem[name + "h"] = (byte)(addr >> 4);
        _m.Mem[name + "l"] = (byte)(addr & 0xF);
    }
}
