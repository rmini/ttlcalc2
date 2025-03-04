using Moq;

namespace TtlCalcSim.Tests;

public class TtlCalcSimTests
{
    public TtlCalcSimTests()
    {
        _alu74181 = new Alu74181();
        _alu = new Alu(_alu74181);
        _inputOutput = new Mock<InputOutput>();
        _mem = new Memory();
        _sim = new TtlCalcSim(_alu, _mem, _inputOutput.Object);
    }

    private readonly Alu74181 _alu74181;
    private readonly Alu _alu;
    private readonly Mock<InputOutput> _inputOutput;
    private readonly Memory _mem;
    private readonly TtlCalcSim _sim;

    [Fact]
    public void HL_Setter_ShouldSucceed()
    {
        _sim.HL = 0xA5;
        Assert.Equal((byte)0xA, (byte)_sim.H);
        Assert.Equal((byte)0x5, (byte)_sim.L);
    }

    [Fact]
    public void HL_Getter_ShouldSucceed()
    {
        _sim.H = 0x9;
        _sim.L = 0xF;
        Assert.Equal((byte)0x9F, _sim.HL);
    }

    [Fact]
    public void PC_Setter_ShouldSucceed()
    {
        _sim.PC = 0x1000;
        Assert.Equal(0, _sim.PC);
    }

    [Fact]
    public void RunStep_NoCond_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation();
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
    }

    [Fact]
    public void RunStep_CondNever_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.Never, Jmp = 2 };
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
    }

    [Fact]
    public void RunStep_CondZ_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.Z, Jmp = 2 };
        _sim.Flags = Flags.EF;
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_CondNZ_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.NZ, Jmp = 2 };
        _sim.Flags = Flags.EF;
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
    }

    [Fact]
    public void RunStep_CondC_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.C, Jmp = 2 };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_CondNC_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.NC, Jmp = 2 };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
    }

    [Fact]
    public void RunStep_CondGT_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.GT, Jmp = 2 };
        _sim.Flags = Flags.None;
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_CondLE_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.LE, Jmp = 2 };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_CondAlways_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.Always, Jmp = 2 };
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_Jmp_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Cond = BranchCond.Always, Jmp = 2 };
        _sim.Prog[2] = new Operation();
        _sim.RunStep();
        Assert.Equal(2, _sim.PC);
    }

    [Fact]
    public void RunStep_SourceX_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.Mem, ZeroPageAddr = true, Imm = 0 };
        _sim.X = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_mem[0]);
    }

    [Fact]
    public void RunStep_AluAdd_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 5 };
        _sim.Prog[1] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 5 };
        _sim.Prog[2] = new Operation { Src = Src.Alu, Dst = Dst.X, AluLogicMode = false, Imm = Alu74181.Add };
        _sim.RunStep();
        _sim.RunStep();
        _sim.RunStep();
        Assert.Equal((byte)10, (byte)_sim.X);
        Assert.Equal(Flags.None, _sim.Flags);
    }

    [Fact]
    public void RunStep_AluSubtractSetsEqualFlag_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 0xA };
        _sim.Prog[1] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 0xA };
        _sim.Prog[2] = new Operation { Src = Src.Alu, Dst = Dst.X, AluLogicMode = false, Imm = Alu74181.Subtract };
        _sim.RunStep();
        _sim.RunStep();
        _sim.RunStep();
        Assert.Equal((byte)0xF, (byte)_sim.X);
        Assert.Equal(Flags.EF, _sim.Flags);
    }

    [Fact]
    public void RunStep_AluAddSetsCarry_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 0xA };
        _sim.Prog[1] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 0xA };
        _sim.Prog[2] = new Operation { Src = Src.Alu, Dst = Dst.X, AluLogicMode = false, Imm = Alu74181.Add };
        _sim.RunStep();
        _sim.RunStep();
        _sim.RunStep();
        Assert.Equal((byte)4, (byte)_sim.X);
        Assert.Equal(Flags.CF, _sim.Flags);
    }

    [Fact]
    public void RunStep_AluSubtract_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 6 };
        _sim.Prog[1] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 5 };
        _sim.Prog[2] = new Operation { Src = Src.Alu, Dst = Dst.X, AluLogicMode = false, DecHLandBcd = true, UseCarryFlagInput = true, Imm = Alu74181.Subtract };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        _sim.RunStep();
        _sim.RunStep();
        Assert.Equal((byte)1, (byte)_sim.X);
        Assert.Equal(Flags.CF, _sim.Flags);
    }

    [Fact]
    public void RunStep_AluSubtractNoBorrowInBorrowOut_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 3 };
        _sim.Prog[1] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 5 };
        _sim.Prog[2] = new Operation { Src = Src.Alu, Dst = Dst.X, AluLogicMode = false, DecHLandBcd = true, UseCarryFlagInput = true, Imm = Alu74181.Subtract };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        _sim.RunStep();
        _sim.RunStep();
        Assert.Equal((byte)8, (byte)_sim.X);
        Assert.Equal(Flags.None, _sim.Flags);
    }

    [Fact]
    public void RunStep_SourceH_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.H, Dst = Dst.Mem, ZeroPageAddr = true, Imm = 0 };
        _sim.H = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_mem[0]);
    }

    [Fact]
    public void RunStep_SourceL_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.L, Dst = Dst.Mem, ZeroPageAddr = true, Imm = 0 };
        _sim.L = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_mem[0]);
    }

    [Fact]
    public void RunStep_SourceIOZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.IO, Dst = Dst.X, ZeroPageAddr = true, Imm = 1 };
        _sim.HL = 0xAA;
        _inputOutput.Setup(i => i.Read(0x01)).Returns(0x5);
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_SourceIONonZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.IO, Dst = Dst.X };
        _sim.HL = 0xAA;
        _inputOutput.Setup(i => i.Read(0xAA)).Returns(0x5);
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_SourceMemZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Mem, Dst = Dst.X, ZeroPageAddr = true, Imm = 0 };
        _mem[0] = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_SourceMemNonZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Mem, Dst = Dst.X };
        _sim.HL = 0xA5;
        _mem[0xA5] = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_SourceFlags_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Flags, Dst = Dst.X };
        _sim.Flags = Flags.EF;
        _sim.RunStep();
        Assert.Equal((byte)Flags.EF, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_SourceImm_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 0x5 };
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_DstX_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.X, Imm = 0x5 };
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.X);
    }

    [Fact]
    public void RunStep_DstY_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.Y, Imm = 0x5 };
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.Y);
    }

    [Fact]
    public void RunStep_DstH_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.H, Imm = 0x5 };
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.H);
    }

    [Fact]
    public void RunStep_DstL_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Imm, Dst = Dst.L, Imm = 0x5 };
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_sim.L);
    }

    [Fact]
    public void RunStep_DstIOZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.IO, ZeroPageAddr = true, Imm = 1 };
        _sim.X = 0x5;
        _sim.HL = 0xAA;
        _sim.RunStep();
        _inputOutput.Verify(i => i.Write(0x01, 0x5));
    }

    [Fact]
    public void RunStep_DstIONonZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.IO };
        _sim.X = 0x5;
        _sim.HL = 0xAA;
        _sim.RunStep();
        _inputOutput.Verify(i => i.Write(0xAA, 0x5));
    }

    [Fact]
    public void RunStep_DstMemZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.Mem, ZeroPageAddr = true, Imm = 0 };
        _sim.X = 0x5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_mem[0]);
    }

    [Fact]
    public void RunStep_DstMemNonZeroPage_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.Mem };
        _sim.X = 0x5;
        _sim.HL = 0xA5;
        _sim.RunStep();
        Assert.Equal(0x5, (byte)_mem[0xA5]);
    }

    [Fact]
    public void RunStep_DstFlags_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.Flags };
        _sim.X = (byte)Flags.EF;
        _sim.RunStep();
        Assert.Equal(Flags.EF, _sim.Flags);
    }

    [Fact]
    public void RunStep_DstNone_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.None };
        _sim.X = 0x5;
        _sim.Y = 0x6;
        _sim.H = 0x7;
        _sim.L = 0x8;
        _sim.Flags = Flags.EF;
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
        Assert.Equal(0x5, (byte)_sim.X);
        Assert.Equal(0x6, (byte)_sim.Y);
        Assert.Equal(0x7, (byte)_sim.H);
        Assert.Equal(0x8, (byte)_sim.L);
        Assert.Equal(Flags.EF, _sim.Flags);
        _inputOutput.VerifyNoOtherCalls();
        Assert.All(Enumerable.Range(0, 256).Select(addr => _mem[(byte)addr]), d => Assert.Equal((byte)0, (byte)d));
    }

    [Fact]
    public void RunStep_IncDecHL_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.None, IncDecHL = true, DecHLandBcd = false };
        _sim.HL = 0x5;
        _sim.RunStep();
        Assert.Equal(0x6, _sim.HL);
    }

    [Fact]
    public void RunStep_DecHL_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.None, IncDecHL = true, DecHLandBcd = true };
        _sim.HL = 0x5;
        _sim.RunStep();
        Assert.Equal(0x4, _sim.HL);
    }

    [Fact]
    public void RunStep_UseCarryFlagInput_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = Dst.None, UseCarryFlagInput = true };
        _sim.Flags = Flags.CF;
        _sim.RunStep();
        Assert.Equal(1, _sim.PC);
    }

    [Fact]
    public void RunStep_AluLogicMode_ShouldSucceed()
    {
        _sim.Prog[0] = new Operation { Src = Src.Alu, Dst = Dst.X, Imm = Alu74181.Xnor, AluLogicMode = true };
        _sim.X = 0b1010;
        _sim.Y = 0b1100;
        _sim.RunStep();
        Assert.Equal((byte)~((Nybble)0b1010 ^ 0b1100), (byte)_sim.X);
    }

    [Fact]
    public void RunStep_AluOperation_ShouldPreserveXFAndYF()
    {
        _sim.Prog[0] = new Operation { Src = Src.Alu, Dst = Dst.X, Imm = Alu74181.Add };
        _sim.X = 5;
        _sim.Y = 3;
        _sim.Flags = Flags.XF | Flags.YF;
        _sim.RunStep();
        Assert.Equal(8, (byte)_sim.X);
        Assert.Equal(Flags.XF | Flags.YF, _sim.Flags);
    }

    [Fact]
    public void RunStep_InvalidCond_ShouldThrowInvalidEnumValueException()
    {
        _sim.Prog[0] = new Operation { Cond = (BranchCond)999 };
        Assert.Throws<InvalidEnumValueException>(() => _sim.RunStep());
    }

    [Fact]
    public void RunStep_InvalidSrc_ShouldThrowInvalidEnumValueException()
    {
        _sim.Prog[0] = new Operation { Src = (Src)999, Dst = Dst.None };
        Assert.Throws<InvalidEnumValueException>(() => _sim.RunStep());
    }

    [Fact]
    public void RunStep_InvalidDst_ShouldThrowInvalidEnumValueException()
    {
        _sim.Prog[0] = new Operation { Src = Src.X, Dst = (Dst)999 };
        Assert.Throws<InvalidEnumValueException>(() => _sim.RunStep());
    }

    [Fact]
    public void LoadProg_InvalidInstructionStart_ShouldThrowArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(new byte[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _sim.LoadProg(stream, (UInt16)(_sim.Prog.Length + 1)));
    }

    [Fact]
    public void LoadProg_InvalidInstructionCount_ShouldThrowArgumentOutOfRangeException()
    {
        using var stream = new MemoryStream(new byte[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _sim.LoadProg(stream, 0, (UInt16)(_sim.Prog.Length + 1)));
    }

    [Fact]
    public void LoadProg_ValidStream_ShouldSucceed()
    {
        using var stream = new MemoryStream(Enumerable.Repeat(new byte[] { 0x01, 0x02 }, 0x1000).SelectMany(x => x).ToArray());
        _sim.LoadProg(stream);
        Assert.All(_sim.Prog, op => Assert.Equal(Operation.FromUInt16(0x0201), op));
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void LoadProg_ValidStreamWithStart_ShouldSucceed()
    {
        using var stream = new MemoryStream(
            Enumerable.Repeat(new byte[] { 0x01, 0x02, 0x03, 0x04 }, _sim.Prog.Length / 2)
                .SelectMany(x => x)
                .Take((_sim.Prog.Length - 1) * 2)
                .ToArray()
            );
        _sim.LoadProg(stream, 1);
        Assert.Equal(new Operation(), _sim.Prog[0]);
        Assert.All(
            _sim.Prog
                .Skip(1)
                .Select((op, i) => (op, i))
                .GroupBy(x => x.i / 2)
                .Select(g => g.Select(x => x.op).ToList())
                .Take((_sim.Prog.Length - 1) / 2),
            ops =>
            {
                Assert.Equal(Operation.FromUInt16(0x0201), ops[0]);
                Assert.Equal(Operation.FromUInt16(0x0403), ops[1]);
            });
        Assert.Equal(Operation.FromUInt16(0x0201), _sim.Prog[^1]);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Fact]
    public void LoadProg_ValidStreamWithStartAndCount_ShouldSucceed()
    {
        using var stream = new MemoryStream([0x01, 0x02, 0x03, 0x04]);
        _sim.LoadProg(stream, 1, 1);
        Assert.Equal(new Operation(), _sim.Prog[0]);
        Assert.Equal(Operation.FromUInt16(0x0201), _sim.Prog[1]);
        Assert.Equal(new Operation(), _sim.Prog[2]);
        Assert.Equal(2, stream.Position);
    }

    [Fact]
    public void LoadProg_EmptyStream_ShouldSucceed()
    {
        using var stream = new MemoryStream([]);
        _sim.LoadProg(stream, 0, 0);
        Assert.All(_sim.Prog, op => Assert.Equal(new Operation(), op));
    }


}