
namespace TtlCalcSim.Tests;

public class AluTests
{
    [Fact]
    public void AddBinary_NoCarryInNoCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(5, 5, 9, false, true, false);
        Assert.Equal((byte)10, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void AddBinary_CarryInNoCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(5, 4, 9, false, false, false);
        Assert.Equal((byte)10, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void AddBinary_NoCarryInCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 8, 9, false, true, false);
        Assert.Equal((byte)0, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void AddBinary_CarryInCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 8, 9, false, false, false);
        Assert.Equal((byte)1, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void AddBcd_NoCarryInNoCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(4, 5, 9, false, true, true);
        Assert.Equal((byte)9, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void AddBcd_CarryInNoCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(4, 4, 9, false, false, true);
        Assert.Equal((byte)9, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void AddBcd_NoCarryInCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 8, 9, false, true, true);
        Assert.Equal((byte)6, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void AddBcd_CarryInCarryOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 8, 9, false, false, true);
        Assert.Equal((byte)7, (byte)f);
        Assert.False(cn4);
    }


    [Fact]
    public void SubtractBinary_NoBorrowInNoBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(6, 5, 6, false, false, false);
        Assert.Equal((byte)1, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void SubtractBinary_BorrowInNoBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(5, 4, 6, false, true, false);
        Assert.Equal((byte)0, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void SubtractBinary_NoBorrowInBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 9, 6, false, false, false);
        Assert.Equal((byte)15, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void SubtractBinary_BorrowInBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 8, 6, false, true, false);
        Assert.Equal((byte)15, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void SubtractBcd_NoBorrowInNoBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(9, 8, 6, false, false, true);
        Assert.Equal((byte)1, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void SubtractBcd_BorrowInNoBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(5, 4, 6, false, true, true);
        Assert.Equal((byte)0, (byte)f);
        Assert.False(cn4);
    }

    [Fact]
    public void SubtractBcd_NoBorrowInBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(8, 9, 6, false, false, true);
        Assert.Equal((byte)9, (byte)f);
        Assert.True(cn4);
    }

    [Fact]
    public void SubtractBcd_BorrowInBorrowOut_ShouldSucceed()
    {
        var alu = new Alu(new Alu74181());
        var (cn4, f) = alu.Evaluate(6, 9, 6, false, true, true);
        Assert.Equal((byte)6, (byte)f);
        Assert.True(cn4);
    }

}