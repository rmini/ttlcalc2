using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace TtlCalcSim;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class TtlCalcSim(Alu alu, InputOutput inputOutput)
{
    public readonly Operation[] Prog = new Operation[1 << 12];
    public readonly Nybble[] Mem = new Nybble[256];

    public int PC
    {
        get;
        set => field = value & 0xfff;
    }

    public Nybble X;
    public Nybble Y;
    public Nybble H;
    public Nybble L;
    public Flags Flags = Flags.None;

    public byte HL
    {
        get => (byte)H << 4 | L;
        set
        {
            H = value >> 4;
            L = value & 0xf;
        }
    }

    public void RunStep()
    {
        var op = Prog[PC++];

        if (op.Cond.HasValue)
        {
            var doJmp = op.Cond switch
            {
                BranchCond.Never => false,
                BranchCond.Z => (Flags & Flags.EF) != 0,
                BranchCond.NZ => (Flags & Flags.EF) == 0,
                BranchCond.C => (Flags & Flags.CF) != 0,
                BranchCond.NC => (Flags & Flags.CF) == 0,
                BranchCond.GT => (Flags & Flags.CF) == 0 && (Flags & Flags.EF) == 0,
                BranchCond.LE => !((Flags & Flags.CF) == 0 && (Flags & Flags.EF) == 0),
                BranchCond.Always => true,
                _ => throw new ArgumentOutOfRangeException(nameof(op.Cond)),
            };
            if (doJmp)
            {
                PC = op.Jmp;
            }
            return;
        }

        bool nCarryIn = !(op.UseCarryFlagInput && (Flags & Flags.CF) != 0);
        byte addr = op.ZeroPageAddr ? op.Imm : HL;

        var (nCarryOut, aluOut) = alu.Evaluate(X, Y, op.Imm, op.AluLogicMode, nCarryIn, op.DecHLandBcd);
        var z = op.Src switch
        {
            Src.X => X,
            Src.Alu => aluOut,
            Src.IO => inputOutput.Read(addr),
            Src.H => H,
            Src.L => L,
            Src.Mem => Mem[addr],
            Src.Flags => (Nybble)(byte)Flags,
            Src.Imm => op.Imm,
            _ => throw new ArgumentOutOfRangeException(nameof(op.Src))
        };

        if (op.Src == Src.Alu)
        {
            Flags =
                (!nCarryOut ? Flags.CF : Flags.None) |
                ((aluOut & 0xf) == 0xf ? Flags.EF : Flags.None) |
                ((Flags & Flags.XF) != 0 ? Flags.XF : Flags.None) |
                ((Flags & Flags.YF) != 0 ? Flags.YF : Flags.None);
        }

        switch (op.Dst)
        {
            case Dst.X:
                X = z;
                break;
            case Dst.Y:
                Y = z;
                break;
            case Dst.H:
                H = z;
                break;
            case Dst.L:
                L = z;
                break;
            case Dst.IO:
                inputOutput.Write(addr, z);
                break;
            case Dst.Mem:
                Mem[addr] = z;
                break;
            case Dst.Flags:
                Flags = (Flags)(byte)z;
                break;
            case Dst.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(op.Dst));
        }

        if (op.IncDecHL)
        {
            if (op.DecHLandBcd)
                HL--;
            else
                HL++;
        }
    }

    public void LoadProg(Stream stream)
    {
        var bytes = new byte[2 * Prog.Length];
            
        if (stream.Read(bytes, 0, bytes.Length) != bytes.Length)
        {
            throw new Exception("Short read");
        }
        for (int loc = 0; loc < Prog.Length; loc++)
        {
            Prog[loc] = new Operation(BitConverter.ToUInt16(bytes, loc * 2));
        }
    }
}