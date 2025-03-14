﻿using System;
using System.Collections.Generic;

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

    public UInt16 ToUInt16()
    {
        if (Cond.HasValue)
            return (UInt16)(Jmp << 4 | (UInt16)Cond.Value << 1 | 0);

        return (UInt16)(
            (UInt16)Imm << 12 |
            (AluLogicMode ? 1 << 11 : 0) |
            (UseCarryFlagInput ? 1 << 10 : 0) |
            (DecHLandBcd ? 1 << 9 : 0) |
            (!IncDecHL ? 1 << 8 : 0) |
            (!ZeroPageAddr ? 1 << 7 : 0) |
            (UInt16)Dst << 4 |
            (UInt16)Src << 1 |
            1);
    }

    public override string ToString()
    {
        return $"Cond: {Cond}, Jmp: {Jmp}, Src: {Src}, Dst: {Dst}, ZeroPageAddr: {ZeroPageAddr}, IncDecHL: {IncDecHL}, DecHLandBcd: {DecHLandBcd}, UseCarryFlagInput: {UseCarryFlagInput}, AluLogicMode: {AluLogicMode}, Imm: {Imm}";
    }

    private static readonly Dictionary<BranchCond, string> BranchCondToString = new()
    {
        { BranchCond.Never, "NOP" },
        { BranchCond.Z, "JZ" },
        { BranchCond.NZ, "JNZ" },
        { BranchCond.C, "JC" },
        { BranchCond.NC, "JNC" },
        { BranchCond.GT, "JGT" },
        { BranchCond.LE, "JLE" },
        { BranchCond.Always, "JMP" },
    };

    private static readonly Dictionary<(bool logic, byte op), string> AluOpToString = new()
    {
        { (false, Alu74181.Subtract), "SUB" },
        { (false, Alu74181.Add), "ADD" },
        { (true,  Alu74181.Or), "OR" },
        { (true,  Alu74181.And), "AND" },
        { (true,  Alu74181.Xor), "XOR" },
        { (true,  Alu74181.Nor), "NOR" },
        { (true,  Alu74181.Nand), "NAND" },
        { (true,  Alu74181.Xnor), "XNOR" },
    };

    public string Disassemble()
    {
        if (Cond.HasValue)
        {
            return BranchCondToString.TryGetValue(Cond.Value, out var jmpInst)
                ? $"{jmpInst} 0x{Jmp:X03}"
                : throw new InvalidEnumValueException(typeof(BranchCond), Cond.Value);
        }

        var hlSuffix = IncDecHL ? $"(HL{(DecHLandBcd ? "--" : "++")})" : "";

        var dst = FormatDst(ref hlSuffix);

        if (Src == Src.Alu) 
            return FormatAluInstruction(dst, hlSuffix);

        var src = FormatSrc(ref hlSuffix);

        return Dst switch
        {
            Dst.None => $"NOP{hlSuffix}",
            _ => $"MOV{hlSuffix} {dst}, {src}"
        };
    }

    private string FormatAluInstruction(string dst, string hlSuffix)
    {
        string mnemonicSuffix = $"{(UseCarryFlagInput ? "C" : "")}{(DecHLandBcd ? "D" : "")}";
        if (AluOpToString.TryGetValue((AluLogicMode, Imm), out var mnemonic))
        {
            return $"{mnemonic}{mnemonicSuffix}{hlSuffix} {dst}, X, Y";
        }

        return
            $"ALU[0b{(AluLogicMode ? 1 : 0):B1}{Imm:B04}]{mnemonicSuffix}{hlSuffix} {dst}, X, Y";
    }

    private string FormatSrc(ref string hlSuffix)
    {
        var src = Src switch
        {
            Src.X => "X",
            Src.Alu => throw new InvalidOperationException("ALU source should be handled by FormatAluInstruction"),
            Src.H => "H",
            Src.L => "L",
            Src.IO => "IO",
            Src.Mem => "Mem",
            Src.Flags => "Flags",
            Src.Imm => $"#0x{Imm:X1}",
            _ => throw new InvalidEnumValueException(typeof(Src), Src)
        };

        if (Src is Src.IO or Src.Mem)
        {
            src += FormatAddressReference(ref hlSuffix);
        }

        return src;
    }

    private string FormatAddressReference(ref string hlSuffix)
    {
        if (ZeroPageAddr)
        {
            return $"[0x{Imm:X1}]";
        }

        hlSuffix = "";
        return $"[HL{(IncDecHL ? (DecHLandBcd ? "--" : "++") : "")}]";
    }

    private string FormatDst(ref string hlSuffix)
    {
        var dst = Dst switch
        {
            Dst.X => "X",
            Dst.Y => "Y",
            Dst.H => "H",
            Dst.L => "L",
            Dst.IO => "IO",
            Dst.Mem => "Mem",
            Dst.Flags => "Flags",
            Dst.None => "NOP",
            _ => throw new InvalidEnumValueException(typeof(Dst), Dst)
        };

        if (Dst is Dst.IO or Dst.Mem)
        {
            dst += FormatAddressReference(ref hlSuffix);
        }

        return dst;
    }
}