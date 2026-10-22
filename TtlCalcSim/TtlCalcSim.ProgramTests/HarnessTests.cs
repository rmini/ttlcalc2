using TtlCalcSim.Testing;

namespace TtlCalcSim.ProgramTests;

/// <summary>
/// Tests of the test harness itself: failure detection and state helpers.
/// </summary>
public class HarnessTests
{
    [Fact]
    public void RunUntil_InfiniteLoop_ThrowsWithTrace()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
            loop:
                mov x, #1
                jmp loop
            done:
                mov x, #2
            """));

        var e = Assert.Throws<ProgramRunException>(() => m.RunUntil("done", maxSteps: 100));

        Assert.Contains("within 100 steps", e.Message);
        Assert.Equal(TestMachine.DefaultTraceLength, e.Trace.Count);
        Assert.Contains("JMP 0x000", e.Message);
    }

    [Fact]
    public void RunUntil_RunsPastEnd_Throws()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                jmp skip
            target:
                mov x, #1
            skip:
                mov x, #2
            """));

        var e = Assert.Throws<ProgramRunException>(() => m.RunUntil("target"));

        Assert.Contains("Ran past the end", e.Message);
    }

    [Fact]
    public void RunUntil_SameLabel_RunsOneIteration()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
            loop:
                mov x, #1
                jmp loop
            """));

        var steps = m.StartAt("loop").RunUntil("loop");

        Assert.Equal(2, steps);
    }

    [Fact]
    public void RunToEnd_ReturnsStepCount()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov x, #1
                mov y, #2
                mov h, #3
            """));

        Assert.Equal(3, m.RunToEnd());
        Assert.Equal(3, m.Steps);
        Assert.Equal(3, m.Program.End);
    }

    [Fact]
    public void IoRead_WithoutInput_Throws()
    {
        var m = new TestMachine(AsmProgram.FromSource("mov x, IO[#5]"));

        var e = Assert.Throws<InvalidOperationException>(() => m.RunToEnd());

        Assert.Contains("IO[0x05]", e.Message);
    }

    [Fact]
    public void IoRead_QueuedValues_AreReadInOrder()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov x, IO[#5]
                mov Mem[#0], x
                mov x, IO[#5]
                mov Mem[#1], x
            """));
        m.QueueInput(5, 7, 9);

        m.RunToEnd();

        Assert.Equal([7, 9], m.Mem.Read(0, 2));
    }

    [Fact]
    public void IoWrites_RecordStepAddressAndValue()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                mov x, #4
                mov IO[#2], x
            """));

        m.RunToEnd();

        Assert.Equal([new IoWrite(2, 2, 4)], m.IoWrites);
    }

    [Fact]
    public void Number_RoundTripsLeastSignificantDigitFirst()
    {
        var m = new TestMachine(AsmProgram.FromSource("nop"));

        m.WriteNumber(0x20, 4, 1234);

        Assert.Equal([4, 3, 2, 1], m.Mem.Read(0x20, 4));
        Assert.Equal(1234, m.ReadNumber(0x20, 4));
    }

    [Fact]
    public void Symbols_IncludeDataAndLocalLabels()
    {
        var p = AsmProgram.FromSource("""
            #bank data
            value: #res 1
            #bank program
            main:
                nop
            .inner:
                nop
            """);

        Assert.Equal(0, p["value"]);
        Assert.Equal(0, p["main"]);
        Assert.Equal(1, p["main.inner"]);
        var e = Assert.Throws<KeyNotFoundException>(() => p["missing"]);
        Assert.Contains("main.inner", e.Message);
    }

    [Fact]
    public void FromSource_InvalidInstruction_ThrowsAssemblerError()
    {
        var e = Assert.Throws<AssemblerException>(() => AsmProgram.FromSource("mov q, #1"));

        Assert.Contains("mov q, #1", e.Message);
    }
}
