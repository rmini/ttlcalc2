namespace TtlCalcSim;

public class Alu74181
{
    // m = false, Arithmetic
    public const byte Subtract = 6;
    public const byte Add = 9;

    // m = true, Logic
    public const byte Or = 14;
    public const byte And = 11;
    public const byte Xor = 6;
    public const byte Nor = 1;
    public const byte Nand = 4;
    public const byte Xnor = 9;

    public (bool cn4, Nybble f) Evaluate(Nybble a, Nybble b, Nybble s, bool m, bool cn)
    {
        var (s0, s1, s2, s3) = UnpackFunctionSelect(s);

        var x = ~(a | (b & s0) | (~b & s1));
        var y = ~((a & ~b & s2) | (a & b & s3));

        var (x0, x1, x2, x3) = UnpackNybble(x);
        var (y0, y1, y2, y3) = UnpackNybble(y);

        bool i0 = !(cn && !m);
        bool i1 = !x0 && y0;
        bool i2 = !((!m && x0) || (!m && y0 && cn));
        bool i3 = !x1 && y1;
        bool i4 = !((!m && x1) || (!m && x0 && y1) || (!m && cn && y0 && y1));
        bool i5 = !x2 && y2;
        bool i6 = !((!m && x2) || (!m && x1 && y2) || (!m && x0 && y1 && y2) || (!m && cn && y0 && y1 && y2));
        bool i7 = !x3 && y3;
        bool i8 = !(cn && y0 && y1 && y2 && y3);
        bool i9 = !((x0 && y1 && y2 && y3) || (x1 && y2 && y3) || (x2 && y3) || x3);
        bool f0 = i0 ^ i1;
        bool f1 = i2 ^ i3;
        bool f2 = i4 ^ i5;
        bool f3 = i6 ^ i7;
        //bool ab = f0 && f1 && f2 && f3;
        //bool p = !(y0 && y1 && y2 && y3);
        bool cn4 = !i8 || !i9;
        //bool g = i9;
        return (cn4, PackNybble(f3, f2, f1, f0));
    }

    private static Nybble PackNybble(bool f3, bool f2, bool f1, bool f0)
    {
        return (f3 ? 8 : 0) + (f2 ? 4 : 0) + (f1 ? 2 : 0) + (f0 ? 1 : 0);
    }

    private static (bool x0, bool x1, bool x2, bool x3) UnpackNybble(Nybble x)
    {
        bool x0 = (x & 1) != 0;
        bool x1 = (x & 2) != 0;
        bool x2 = (x & 4) != 0;
        bool x3 = (x & 8) != 0;
        return (x0, x1, x2, x3);
    }

    private static (Nybble s0, Nybble s1, Nybble s2, Nybble s3) UnpackFunctionSelect(Nybble s)
    {
        var s0 = (s & 1) != 0 ? 0xf : 0;
        var s1 = (s & 2) != 0 ? 0xf : 0;
        var s2 = (s & 4) != 0 ? 0xf : 0;
        var s3 = (s & 8) != 0 ? 0xf : 0;
        return (s0, s1, s2, s3);
    }
}