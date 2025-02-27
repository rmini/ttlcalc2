using System;

namespace TtlCalcSim;

public enum BranchCond
{
    Never,
    Z,
    NZ,
    C,
    NC,
    GT,
    LE,
    Always
}

public enum Src
{
    X,
    Alu,
    H,
    L,
    IO,
    Mem,
    Flags,
    Imm
}

public enum Dst
{
    X,
    Y,
    H,
    L,
    IO,
    Mem,
    Flags,
    None
}

[Flags]
public enum Flags
{
    None = 0,
    CF = 1 << 0,
    EF = 1 << 1,
    XF = 1 << 2,
    YF = 1 << 3
}

public readonly struct Operation
{
    public readonly BranchCond? Cond;
    public readonly UInt16 Jmp;
    public readonly Src Src;
    public readonly Dst Dst;
    public readonly bool ZeroPageAddr;
    public readonly bool IncDecHL;
    public readonly bool DecHLandBcd;
    public readonly bool UseCarryFlagInput;
    public readonly bool AluLogicMode;
    public readonly Nybble Imm;

    public Operation(UInt16 d)
    {
        bool isJmp = Slice(d, 0, 0) == 0;
        if (isJmp)
        {
            Cond = (BranchCond)Slice(d, 3, 1);
            Jmp = Slice(d, 15, 4);
        }
        else
        {
            Src = (Src)Slice(d, 3, 1);
            Dst = (Dst)Slice(d, 6, 4);
            ZeroPageAddr = Slice(d, 7, 7) == 0;
            IncDecHL = Slice(d, 8, 8) == 0;
            DecHLandBcd = Slice(d, 9, 9) != 0;
            UseCarryFlagInput = Slice(d, 10, 10) != 0;
            AluLogicMode = Slice(d, 11, 11) != 0;
            Imm = Slice(d, 15, 12);
        }
    }

    private static UInt16 Slice(UInt16 d, byte hi, byte lo)
    {
        return (UInt16)((d >> lo) & ((1 << (hi - lo + 1)) - 1));
    }

    public override string ToString()
    {
        return $"Cond: {Cond}, Jmp: {Jmp}, Src: {Src}, Dst: {Dst}, ZeroPageAddr: {ZeroPageAddr}, IncDecHL: {IncDecHL}, DecHLandBcd: {DecHLandBcd}, UseCarryFlagInput: {UseCarryFlagInput}, AluLogicMode: {AluLogicMode}, Imm: {Imm}";
    }
}