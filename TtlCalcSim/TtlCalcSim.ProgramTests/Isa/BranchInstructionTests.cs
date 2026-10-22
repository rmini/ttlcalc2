using TtlCalcSim.Testing;
using static TtlCalcSim.ProgramTests.Isa.IsaReference;

namespace TtlCalcSim.ProgramTests.Isa;

/// <summary>
/// End-to-end branch tests: every condition against every flag combination, jump targets,
/// and comparisons built from SUB.
/// </summary>
public class BranchInstructionTests
{
    public static IEnumerable<object[]> AllConditions => Conditions.Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(AllConditions))]
    public void Condition_MatchesConditionTable(string cond)
    {
        var program = AsmProgram.FromSource(BranchProgram($"j{cond}"));
        var mismatches = new Mismatches($"j{cond}");

        for (var flags = 0; flags < 16; flags++)
        {
            var m = new TestMachine(program, traceLength: 0) { Flags = (Flags)flags };
            m.RunToEnd(maxSteps: 10);
            mismatches.Expect(BranchTaken(cond, (Flags)flags), m.L == 1, $"Flags={(Flags)flags} taken");
        }

        mismatches.AssertNone();
    }

    [Fact]
    public void Nop_NeverBranches()
    {
        for (var flags = 0; flags < 16; flags++)
        {
            var m = new TestMachine(AsmProgram.FromSource("nop\nmov x, #1")) { Flags = (Flags)flags };

            Assert.Equal(2, m.RunToEnd());
            Assert.Equal(1, m.X);
        }
    }

    [Fact]
    public void Jump_ReachesFull12BitAddress()
    {
        var m = new TestMachine(AsmProgram.FromSource("""
                jmp far
                mov x, #1
            #addr 0xFFE
            far:
                mov y, #2
            """));

        m.RunUntil("far");
        m.Step();

        Assert.Equal(0xFFF, m.PC);
        Assert.Equal(0, m.X);
        Assert.Equal(2, m.Y);
    }

    /// <summary>
    /// impl.md: SUB without carry computes X - Y - 1, so equal operands give all ones (EF), and
    /// the carry flag "can be used to determine if Y is less than, greater than, or equal to X".
    /// The branch mnemonics therefore compare Y against X.
    /// </summary>
    [Theory]
    [InlineData("e")]
    [InlineData("ne")]
    [InlineData("lt")]
    [InlineData("ge")]
    [InlineData("gt")]
    [InlineData("le")]
    public void SubCompare_BranchesCompareYAgainstX(string cond)
    {
        var program = AsmProgram.FromSource("sub None, x, y\n" + BranchProgram($"j{cond}"));
        var mismatches = new Mismatches($"sub None, x, y; j{cond}");

        for (var x = 0; x < 16; x++)
        for (var y = 0; y < 16; y++)
        {
            var expected = cond switch
            {
                "e" => y == x,
                "ne" => y != x,
                "lt" => y < x,
                "ge" => y >= x,
                "gt" => y > x,
                "le" => y <= x,
                _ => throw new ArgumentOutOfRangeException(nameof(cond)),
            };
            var m = new TestMachine(program, traceLength: 0) { X = (byte)x, Y = (byte)y };
            m.RunToEnd(maxSteps: 10);
            mismatches.Expect(expected, m.L == 1, $"X={x} Y={y} taken");
        }

        mismatches.AssertNone();
    }

    /// <summary>Sets L to 1 if the branch is taken and 0 otherwise.</summary>
    private static string BranchProgram(string jump) => $"""
            {jump} taken
            mov l, #0
            jmp done
        taken:
            mov l, #1
        done:
        """;
}
