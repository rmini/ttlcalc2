
namespace TtlCalcSim.Tests;

public class OperationTests
{
    [Fact]
    public void Constructor_JmpOperation_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1110_1110_1001_0010; // Example data for Jmp operation
        Operation operation = Operation.FromUInt16(data);

        Assert.Equal(BranchCond.Z, operation.Cond);
        Assert.Equal((UInt16)0b1110_1110_1001, operation.Jmp);
    }

    [Fact]
    public void Constructor_NonJmpOperation_ShouldInitializeCorrectly()
    {
        UInt16 data = 0b1010_0000_0000_0001; // Example data for non-Jmp operation
        Operation operation = Operation.FromUInt16(data);

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
        Operation operation = Operation.FromUInt16(data);

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
        Operation operation = Operation.FromUInt16(data);

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
        Operation operation = Operation.FromUInt16(data);

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
        Operation operation = Operation.FromUInt16(data);
        string result = operation.ToString();

        string expected = "Cond: , Jmp: 0, Src: X, Dst: X, ZeroPageAddr: True, IncDecHL: True, DecHLandBcd: False, UseCarryFlagInput: False, AluLogicMode: False, Imm: 8";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Disassemble_Jmp_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Cond = BranchCond.Z,
            Jmp = 0b1110_1110_1001
        };
        string result = operation.Disassemble();
        Assert.Equal("JZ 0xEE9", result);
    }

    [Fact]
    public void Disassemble_JmpNever_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Cond = BranchCond.Never,
            Jmp = 0b1110_1110_1001
        };
        string result = operation.Disassemble();
        Assert.Equal("NOP 0xEE9", result);
    }

    [Fact]
    public void Disassemble_AluAdd_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Alu,
            Dst = Dst.X,
            DecHLandBcd = false,
            UseCarryFlagInput = false,
            AluLogicMode = false,
            Imm = Alu74181.Add
        };
        string result = operation.Disassemble();
        Assert.Equal("ADD X, X, Y", result);
    }

    [Fact]
    public void Disassemble_AluAddCarryBcd_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Alu,
            Dst = Dst.Mem,
            IncDecHL = false,
            DecHLandBcd = true,
            UseCarryFlagInput = true,
            AluLogicMode = false,
            Imm = Alu74181.Add
        };
        string result = operation.Disassemble();
        Assert.Equal("ADDCD Mem[HL], X, Y", result);
    }

    [Fact]
    public void Disassemble_MovYH_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.H,
            Dst = Dst.Y
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV Y, H", result);
    }

    [Fact]
    public void Disassemble_MovMemLIncHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.L,
            Dst = Dst.Mem,
            ZeroPageAddr = false,
            IncDecHL = true
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV Mem[HL++], L", result);
    }

    [Fact]
    public void Disassemble_MovMemFlagsZeroPage_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Flags,
            Dst = Dst.Mem,
            ZeroPageAddr = true,
            IncDecHL = false
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV Mem[0x0], Flags", result);
    }

    [Fact]
    public void Disassemble_MovIOImmDecHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Imm,
            Dst = Dst.IO,
            ZeroPageAddr = false,
            IncDecHL = true,
            DecHLandBcd = true,
            Imm = 0b1111
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV IO[HL--], #0xF", result);
    }

    [Fact]
    public void Disassemble_MovMemIOZeroPageIncHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.IO,
            Dst = Dst.Mem,
            ZeroPageAddr = true,
            IncDecHL = true
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV Mem[0x0], IO[0x0]; HL++", result);
    }

    [Fact]
    public void Disassemble_MovXMemDecHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Mem,
            Dst = Dst.X,
            ZeroPageAddr = false,
            IncDecHL = true,
            DecHLandBcd = true
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV X, Mem[HL--]", result);
    }

    [Fact]
    public void Disassemble_MovXMemIncHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Mem,
            Dst = Dst.X,
            ZeroPageAddr = false,
            IncDecHL = true,
            DecHLandBcd = false
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV X, Mem[HL++]", result);
    }

    [Fact]
    public void Disassemble_MovXMemHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Mem,
            Dst = Dst.X,
            ZeroPageAddr = false,
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV X, Mem[HL]", result);
    }

    [Fact]
    public void Disassemble_WeirdAluLogicOp_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Alu,
            Dst = Dst.X,
            IncDecHL = false,
            UseCarryFlagInput = true,
            AluLogicMode = false,
            DecHLandBcd = true,
            Imm = 0b1111
        };
        string result = operation.Disassemble();
        Assert.Equal("ALU[0b01111]CD X, X, Y", result);
    }

    [Fact]
    public void Disassembly_WeirdAluArithOp_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Alu,
            Dst = Dst.X,
            IncDecHL = false,
            UseCarryFlagInput = true,
            AluLogicMode = true,
            DecHLandBcd = false,
            Imm = 0b1111
        };
        string result = operation.Disassemble();
        Assert.Equal("ALU[0b11111]C X, X, Y", result);
    }

    [Fact]
    public void Disassemble_MovHX_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.X,
            Dst = Dst.H
        };

        string result = operation.Disassemble();
        Assert.Equal("MOV H, X", result);
    }

    [Fact]
    public void Disassemble_NopIncHL_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Dst = Dst.None,
            IncDecHL = true
        };

        string result = operation.Disassemble();
        Assert.Equal("NOP; HL++", result);
    }

    [Fact]
    public void Disassemble_MovFlagsImm_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Imm,
            Dst = Dst.Flags,
            Imm = 0b1111
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV Flags, #0xF", result);
    }

    [Fact]
    public void Disassemble_MovLImm_ShouldReturnCorrectString()
    {
        Operation operation = new()
        {
            Src = Src.Imm,
            Dst = Dst.L,
            Imm = 0b1010
        };
        string result = operation.Disassemble();
        Assert.Equal("MOV L, #0xA", result);
    }

    [Fact]
    public void Disassemble_InvalidDst_ThrowsInvalidEnumValueException()
    {
        Operation operation = new()
        {
            Dst = (Dst)0xFF
        };
        Assert.Throws<InvalidEnumValueException>(() => operation.Disassemble());
    }

    [Fact]
    public void Disassemble_InvalidSrc_ThrowsInvalidEnumValueException()
    {
        Operation operation = new()
        {
            Src = (Src)0xFF
        };
        Assert.Throws<InvalidEnumValueException>(() => operation.Disassemble());
    }
}