
namespace TtlCalcSim.Tests;

public class OperationTests
{
    [Fact]
    public void Constructor_JmpOperation_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1110_1110_1001_0010; // Example data for Jmp operation
        Operation operation = new Operation(data);

        Assert.Equal(BranchCond.Z, operation.Cond);
        Assert.Equal((UInt16)0b1110_1110_1001, operation.Jmp);
    }

    [Fact]
    public void Constructor_NonJmpOperation_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1010_0000_0000_0001; // Example data for non-Jmp operation
        Operation operation = new Operation(data);

        Assert.Equal(Src.X, operation.Src);
        Assert.Equal(Dst.X, operation.Dst);
        Assert.True(operation.ZeroPageAddr);
        Assert.True(operation.IncDecHL);
        Assert.False(operation.DecHLandBcd);
        Assert.False(operation.UseCarryFlagInput);
        Assert.False(operation.AluLogicMode);
        Assert.Equal((Nybble)0b1010, operation.Imm);
    }

    [Fact]
    public void Constructor_NonJmpOperation_WithDifferentValues_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1111_0110_1110_1011; // Example data for non-Jmp operation with different values
        Operation operation = new Operation(data);

        Assert.Equal(Src.Mem, operation.Src);
        Assert.Equal(Dst.Flags, operation.Dst);
        Assert.False(operation.ZeroPageAddr);
        Assert.True(operation.IncDecHL);
        Assert.True(operation.DecHLandBcd);
        Assert.True(operation.UseCarryFlagInput);
        Assert.False(operation.AluLogicMode);
        Assert.Equal((Nybble)0b1111, operation.Imm);
    }

    [Fact]
    public void Constructor_NonJmpOperation_WithAllTrueFlags_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1111_1111_1111_1111; // Example data for non-Jmp operation with all true flags
        Operation operation = new Operation(data);

        Assert.Equal(Src.Imm, operation.Src);
        Assert.Equal(Dst.None, operation.Dst);
        Assert.False(operation.ZeroPageAddr);
        Assert.False(operation.IncDecHL);
        Assert.True(operation.DecHLandBcd);
        Assert.True(operation.UseCarryFlagInput);
        Assert.True(operation.AluLogicMode);
        Assert.Equal((Nybble)0b1111, operation.Imm);
    }

    [Fact]
    public void Constructor_NonJmpOperation_WithAllFalseFlags_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b0000_0000_0000_0001; // Example data for non-Jmp operation with all false flags
        Operation operation = new Operation(data);

        Assert.Equal(Src.X, operation.Src);
        Assert.Equal(Dst.X, operation.Dst);
        Assert.True(operation.ZeroPageAddr);
        Assert.True(operation.IncDecHL);
        Assert.False(operation.DecHLandBcd);
        Assert.False(operation.UseCarryFlagInput);
        Assert.False(operation.AluLogicMode);
        Assert.Equal((Nybble)0b0000, operation.Imm);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectString()
    {
        UInt16 data = 0b1000_0000_0000_0001; // Example data for non-Jmp operation
        Operation operation = new Operation(data);
        string result = operation.ToString();

        string expected = "Cond: , Jmp: 0, Src: X, Dst: X, ZeroPageAddr: True, IncDecHL: True, DecHLandBcd: False, UseCarryFlagInput: False, AluLogicMode: False, Imm: 8";
        Assert.Equal(expected, result);
    }
}