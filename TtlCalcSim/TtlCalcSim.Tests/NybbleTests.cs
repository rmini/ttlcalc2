
namespace TtlCalcSim.Tests;

public class NybbleTests
{
    [Fact]
    public void ImplicitConversionFromInt_ValidValue_ShouldSucceed()
    {
        Nybble nybble = 5;
        Assert.Equal((byte)5, (byte)nybble);
    }

    [Fact]
    public void ImplicitConversionFromInt_InvalidValue_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (Nybble)16);
    }

    [Fact]
    public void ImplicitConversionFromInt_InvalidNegativeValue_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => (Nybble)(-1));
    }

    [Fact]
    public void ImplicitConversionToByte_ShouldSucceed()
    {
        Nybble nybble = 7;
        byte value = nybble;
        Assert.Equal((byte)7, value);
    }

    [Fact]
    public void BitwiseNotOperator_ShouldSucceed()
    {
        Nybble nybble = 0b1010;
        Nybble result = ~nybble;
        Assert.Equal((byte)0b0101, (byte)result);
    }

    [Fact]
    public void BitwiseAndOperator_ShouldSucceed()
    {
        Nybble a = 0b1100;
        Nybble b = 0b1010;
        Nybble result = a & b;
        Assert.Equal((byte)0b1000, (byte)result);
    }

    [Fact]
    public void BitwiseOrOperator_ShouldSucceed()
    {
        Nybble a = 0b1100;
        Nybble b = 0b1010;
        Nybble result = a | b;
        Assert.Equal((byte)0b1110, (byte)result);
    }

    [Fact]
    public void BitwiseXorOperator_ShouldSucceed()
    {
        Nybble a = 0b1100;
        Nybble b = 0b1010;
        Nybble result = a ^ b;
        Assert.Equal((byte)0b0110, (byte)result);
    }

    [Fact]
    public void AdditionOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble b = 3;
        Nybble result = a + b;
        Assert.Equal((byte)8, (byte)result);
    }

    [Fact]
    public void AdditionOperator_Overflow_ShouldSucceed()
    {
        Nybble a = 15;
        Nybble b = 1;
        Nybble result = a + b;
        Assert.Equal((byte)0, (byte)result);
    }

    [Fact]
    public void SubtractionOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble b = 3;
        Nybble result = a - b;
        Assert.Equal((byte)2, (byte)result);
    }

    [Fact]
    public void SubtractionOperator_Underflow_ShouldSucceed()
    {
        Nybble a = 0;
        Nybble b = 1;
        Nybble result = a - b;
        Assert.Equal((byte)15, (byte)result);
    }

    [Fact]
    public void MultiplicationOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble b = 3;
        Nybble result = a * b;
        Assert.Equal((byte)15, (byte)result);
    }

    [Fact]
    public void MultiplicationOperator_Overflow_ShouldSucceed()
    {
        Nybble a = 8;
        Nybble b = 2;
        Nybble result = a * b;
        Assert.Equal((byte)0, (byte)result);
    }

    [Fact]
    public void DivisionOperator_ShouldSucceed()
    {
        Nybble a = 6;
        Nybble b = 3;
        Nybble result = a / b;
        Assert.Equal((byte)2, (byte)result);
    }

    [Fact]
    public void ModulusOperator_ShouldSucceed()
    {
        Nybble a = 7;
        Nybble b = 3;
        Nybble result = a % b;
        Assert.Equal((byte)1, (byte)result);
    }

    [Fact]
    public void IncrementOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble result = ++a;
        Assert.Equal((byte)6, (byte)result);
    }

    [Fact]
    public void IncrementOperator_Overflow_ShouldSucceed()
    {
        Nybble a = 15;
        Nybble result = ++a;
        Assert.Equal((byte)0, (byte)result);
    }

    [Fact]
    public void PostIncrementOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble result = a++;
        Assert.Equal((byte)5, (byte)result);
        Assert.Equal((byte)6, (byte)a);
    }

    [Fact]
    public void PostIncrementOperator_Overflow_ShouldSucceed()
    {
        Nybble a = 15;
        Nybble result = a++;
        Assert.Equal((byte)15, (byte)result);
        Assert.Equal((byte)0, (byte)a);
    }

    [Fact]
    public void DecrementOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble result = --a;
        Assert.Equal((byte)4, (byte)result);
    }

    [Fact]
    public void DecrementOperator_Underflow_ShouldSucceed()
    {
        Nybble a = 0;
        Nybble result = --a;
        Assert.Equal((byte)15, (byte)result);
    }

    [Fact]
    public void PostDecrementOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble result = a--;
        Assert.Equal((byte)5, (byte)result);
        Assert.Equal((byte)4, (byte)a);
    }

    [Fact]
    public void PostDecrementOperator_Underflow_ShouldSucceed()
    {
        Nybble a = 0;
        Nybble result = a--;
        Assert.Equal((byte)0, (byte)result);
        Assert.Equal((byte)15, (byte)a);
    }

    [Fact]
    public void LeftShiftOperator_ShouldSucceed()
    {
        Nybble a = 1;
        Nybble result = a << 2;
        Assert.Equal((byte)4, (byte)result);
    }

    [Fact]
    public void LeftShiftOperator_Overflow_ShouldSucceed()
    {
        Nybble a = 8;
        Nybble result = a << 2;
        Assert.Equal((byte)0, (byte)result);
    }

    [Fact]
    public void RightShiftOperator_ShouldSucceed()
    {
        Nybble a = 4;
        Nybble result = a >> 2;
        Assert.Equal((byte)1, (byte)result);
    }

    [Fact]
    public void RightShiftOperator_Underflow_ShouldSucceed()
    {
        Nybble a = 4;
        Nybble result = a >> 3;
        Assert.Equal((byte)0, (byte)result);
    }

    [Fact]
    public void LessThanOperator_ShouldSucceed()
    {
        Nybble a = 3;
        Nybble b = 5;
        Assert.True(a < b);
    }

    [Fact]
    public void GreaterThanOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble b = 3;
        Assert.True(a > b);
    }

    [Fact]
    public void LessThanOrEqualOperator_ShouldSucceed()
    {
        Nybble a = 3;
        Nybble b = 5;
        Nybble c = 3;
        Assert.True(a <= b);
        Assert.True(a <= c);
    }

    [Fact]
    public void GreaterThanOrEqualOperator_ShouldSucceed()
    {
        Nybble a = 5;
        Nybble b = 3;
        Nybble c = 5;
        Assert.True(a >= b);
        Assert.True(a >= c);
    }

    [Fact]
    public void GetHashCode_ShouldSucceed()
    {
        Nybble a = 5;
        int hashCode = a.GetHashCode();
        Assert.Equal(-1584136870 + 5.GetHashCode(), hashCode);
    }

    [Fact]
    public void ToString_ShouldSucceed()
    {
        Nybble a = 5;
        string result = a.ToString();
        Assert.Equal("5", result);
    }

    [Fact]
    public void ToString_FormatProvider_ShouldSucceed()
    {
        Nybble a = 5;
        string result = a.ToString(null, null);
        Assert.Equal("5", result);
    }

    [Fact]
    public void ToString_Format_ShouldSucceed()
    {
        Nybble a = 15;
        string result = a.ToString("X", null);
        Assert.Equal("F", result);
    }
}