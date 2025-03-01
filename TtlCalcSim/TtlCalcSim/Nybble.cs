using System;

namespace TtlCalcSim;

public readonly record struct Nybble : IFormattable
{
    private readonly byte _value;

    private Nybble(int value)
    {
        _value = (byte)(value & 0xf);
    }

    public static implicit operator Nybble(int value) => new Nybble(value is >= 0 and <= 15 ? (byte)value : throw new ArgumentOutOfRangeException(nameof(value), "Nybble must be 0 <= value <= 15"));
    public static implicit operator byte(Nybble nybble) => nybble._value;
    public static Nybble operator ~(Nybble nybble) => new Nybble(~nybble._value);
    public static Nybble operator &(Nybble a, Nybble b) => new Nybble(a._value & b._value);
    public static Nybble operator |(Nybble a, Nybble b) => new Nybble(a._value | b._value);
    public static Nybble operator ^(Nybble a, Nybble b) => new Nybble(a._value ^ b._value);
    public static Nybble operator +(Nybble a, Nybble b) => new Nybble(a._value + b._value);
    public static Nybble operator -(Nybble a, Nybble b) => new Nybble(a._value - b._value);
    public static Nybble operator *(Nybble a, Nybble b) => new Nybble(a._value * b._value);
    public static Nybble operator /(Nybble a, Nybble b) => new Nybble(a._value / b._value);
    public static Nybble operator %(Nybble a, Nybble b) => new Nybble(a._value % b._value);
    public static Nybble operator ++(Nybble a) => new Nybble(a._value + 1);
    public static Nybble operator --(Nybble a) => new Nybble(a._value - 1);
    public static Nybble operator <<(Nybble a, int b) => new Nybble(a._value << b);
    public static Nybble operator >> (Nybble a, int b) => new Nybble(a._value >> b);
    public static bool operator <(Nybble a, Nybble b) => a._value < b._value;
    public static bool operator >(Nybble a, Nybble b) => a._value > b._value;
    public static bool operator <=(Nybble a, Nybble b) => a._value <= b._value;
    public static bool operator >=(Nybble a, Nybble b) => a._value >= b._value;
    public override int GetHashCode() => -1584136870 + _value.GetHashCode();
    public override string ToString() => _value.ToString();
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return _value.ToString(format, formatProvider);
    }
}