
namespace TtlCalcSim.Tests;

public class Alu74181Tests
{
    [Fact]
    public void Evaluate_ShouldSucceed()
    {
        var alu74181 = new Alu74181();
        var (cn4, f) = alu74181.Evaluate(0, 0, 0, true, false);
        Assert.Equal((byte)0xf, (byte)f);
    }

    public static IEnumerable<object[]> EvaluateLogicTestData()
    {
        for (byte a = 0; a < 16; a++)
        {
            for (byte b = 0; b < 16; b++)
            {
                Nybble na = a;
                Nybble nb = b;

                yield return [na, nb, 0, true, false, ~na];
                yield return [na, nb, 1, true, false, ~(na | nb)];
                yield return [na, nb, 2, true, false, ~na & nb];
                yield return [na, nb, 3, true, false, (Nybble)0];
                yield return [na, nb, 4, true, false, ~(na & nb)];
                yield return [na, nb, 5, true, false, ~nb];
                yield return [na, nb, 6, true, false, na ^ nb];
                yield return [na, nb, 7, true, false, na & ~nb];
                yield return [na, nb, 8, true, false, ~na | nb];
                yield return [na, nb, 9, true, false, ~(na ^ nb)];
                yield return [na, nb, 10, true, false, nb];
                yield return [na, nb, 11, true, false, na & nb];
                yield return [na, nb, 12, true, false, (Nybble)0xf];
                yield return [na, nb, 13, true, false, na | ~nb];
                yield return [na, nb, 14, true, false, na | nb];
                yield return [na, nb, 15, true, false, na];
            }
        }
    }

    [Theory]
    [MemberData(nameof(EvaluateLogicTestData))]
    public void EvaluateLogic_ShouldSucceed(Nybble a, Nybble b, Nybble s, bool m, bool cn, Nybble expected)
    {
        var alu74181 = new Alu74181();
        var (cn4, f) = alu74181.Evaluate(a, b, s, m, cn);
        Assert.Equal((byte)expected, (byte)f);
    }

    public static IEnumerable<object[]> EvaluateArithmeticTestData()
    {
        for (byte ab = 0; ab < 16; ab++)
        {
            for (byte bb = 0; bb < 16; bb++)
            {
                Nybble a = ab;
                Nybble b = bb;
                yield return [a, b, 0, true, a, true];
                yield return [a, b, 0, false, a + 1, !(a == 15)];
                yield return [a, b, 1, true, a | b, true];
                yield return [a, b, 1, false, (a | b) + 1, !((byte)(a | b) + 1 > 15)];
                yield return [a, b, 2, true, a | ~b, true];
                yield return [a, b, 2, false, (a | ~b) + 1, !((byte)(a | ~b) + 1 > 15)];
                yield return [a, b, 3, true, 15, true];
                yield return [a, b, 3, false, 0, false];
                yield return [a, b, 4, true, a + (a & ~b), !(ab + (byte)(a & ~b) > 15)];
                yield return [a, b, 4, false, (a + (a & ~b)) + 1, !(ab + (byte)(a & ~b) + 1 > 15)];
                yield return [a, b, 5, true, (a | b) + (a & ~b), !((byte)(a | b) + (byte)(a & ~b) > 15)];
                yield return [a, b, 5, false, ((a | b) + (a & ~b)) + 1, !((byte)(a | b) + (byte)(a & ~b) + 1 > 15)];
                yield return [a, b, 6, true, a - b - 1, !(a > b)];
                yield return [a, b, 6, false, a - b, !(a >= b)];
                yield return [a, b, 7, true, (a & ~b) - 1, !((a & ~b) - 1 != 15)];
                yield return [a, b, 7, false, a & ~b, false];
                yield return [a, b, 8, true, a + (a & b), !(ab + (byte)(a & b) > 15)];
                yield return [a, b, 8, false, a + (a & b) + 1, !(ab + (byte)(a & b) + 1 > 15)];
                yield return [a, b, 9, true, a + b, !(ab + bb > 15)];
                yield return [a, b, 9, false, a + b + 1, !(ab + bb + 1 > 15)];
                yield return [a, b, 10, true, (a | ~b) + (a & b), !((byte)(a | ~b) + (byte)(a & b) > 15)];
                yield return [a, b, 10, false, (a | ~b) + (a & b) + 1, !((byte)(a | ~b) + (byte)(a & b) + 1 > 15)];
                yield return [a, b, 11, true, (a & b) - 1, (a & b) == 0];
                yield return [a, b, 11, false, a & b, false];
                yield return [a, b, 12, true, a + a, !(a > 7)];
                yield return [a, b, 12, false, a + a + 1, !(a > 7)];
                yield return [a, b, 13, true, (a | b) + a, !((byte)(a | b) + ab > 15)];
                yield return [a, b, 13, false, (a | b) + a + 1, !((byte)(a | b) + ab + 1 > 15)];
                yield return [a, b, 14, true, (a | ~b) + a, !((byte)(a | ~b) + ab > 15)];
                yield return [a, b, 14, false, (a | ~b) + a + 1, !((byte)(a | ~b) + ab + 1 > 15)];
                yield return [a, b, 15, true, a - 1, a == 0];
                yield return [a, b, 15, false, a, false];
            }
        }
    }

    [Theory]
    [MemberData(nameof(EvaluateArithmeticTestData))]
    public void EvaluateArithmetic_ShouldSucceed(Nybble a, Nybble b, Nybble s, bool cn, Nybble expected,
        bool expectedCn4)
    {
        var alu74181 = new Alu74181();
        var (cn4, f) = alu74181.Evaluate(a, b, s, false, cn);
        Assert.Equal((byte)expected, (byte)f);
        Assert.Equal(expectedCn4, cn4);
    }
}