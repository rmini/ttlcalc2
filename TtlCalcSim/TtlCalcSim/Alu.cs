namespace TtlCalcSim;

public class Alu(Alu74181 alu74181)
{
    public (bool cn4, Nybble f) Evaluate(Nybble a, Nybble b, Nybble s, bool m, bool cn, bool bcd)
    {
        var bcdPreCorrect = bcd && ((s & 8) != 0);
        if (bcdPreCorrect)
        {
            b = b + 6;
        }

        (var cn4, var f) = alu74181.Evaluate(a, b, s, m, cn);

        var bcdPostCorrect = bcd && cn4;
        if (bcdPostCorrect)
        {
            f += 10;
        }

        return (cn4, f);
    }
}