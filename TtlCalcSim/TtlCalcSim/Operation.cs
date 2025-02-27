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

public readonly record struct Operation
{
    public BranchCond? Cond { get; init; }
    public UInt16 Jmp { get; init; }
    public Src Src { get; init; }
    public Dst Dst { get; init; }
    public bool ZeroPageAddr { get; init; }
    public bool IncDecHL { get; init; }
    public bool DecHLandBcd { get; init; }
    public bool UseCarryFlagInput { get; init; }
    public bool AluLogicMode { get; init; }
    public Nybble Imm { get; init; }

    public static Operation FromUInt16(UInt16 d)
    {
        var isJmp = Slice(d, 0, 0) == 0;
        return isJmp switch
        {
            true => new Operation
            {
                Cond = (BranchCond)Slice(d, 3, 1),
                Jmp = Slice(d, 15, 4)
            },
            false => new Operation
            {
                Src = (Src)Slice(d, 3, 1),
                Dst = (Dst)Slice(d, 6, 4),
                ZeroPageAddr = Slice(d, 7, 7) == 0,
                IncDecHL = Slice(d, 8, 8) == 0,
                DecHLandBcd = Slice(d, 9, 9) != 0,
                UseCarryFlagInput = Slice(d, 10, 10) != 0,
                AluLogicMode = Slice(d, 11, 11) != 0,
                Imm = Slice(d, 15, 12),
            }
        };
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