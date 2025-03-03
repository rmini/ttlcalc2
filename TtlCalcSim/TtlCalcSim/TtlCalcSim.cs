using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace TtlCalcSim;

public class TtlCalcSim(Alu alu, Memory mem, InputOutput inputOutput)
{
    public readonly Operation[] Prog = new Operation[1 << 12];

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
        get => (byte)((byte)H << 4 | (byte)L);
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
            Jump(op);
            return;
        }

        byte addr = op.ZeroPageAddr ? op.Imm : HL;

        (var result, Flags) = op.Src switch
        {
            Src.X => (X, Flags),
            Src.Alu => ProcessAlu(op),
            Src.IO => (inputOutput.Read(addr), Flags),
            Src.H => (H, Flags),
            Src.L => (L, Flags),
            Src.Mem => (mem[addr], Flags),
            Src.Flags => ((Nybble)(byte)Flags, Flags),
            Src.Imm => (op.Imm, Flags),
            _ => throw new ArgumentOutOfRangeException(nameof(op.Src))
        };

        StoreResult(op, result, addr);

        if (op.IncDecHL)
        {
            if (op.DecHLandBcd)
                HL--;
            else
                HL++;
        }
    }

    private (Nybble aluOut, Flags newFlags) ProcessAlu(Operation op)
    {
        bool nCarryIn = !(op.UseCarryFlagInput && (Flags & Flags.CF) != 0);
        var (nCarryOut, aluOut) = alu.Evaluate(X, Y, op.Imm, op.AluLogicMode, nCarryIn, op.DecHLandBcd);
        var newFlags = UpdateFlags(!nCarryOut, (aluOut & 0xf) == 0xf);

        return (aluOut, newFlags);
    }

    private Flags UpdateFlags(bool carryOut, bool equalOut)
    {
        var (_, _, xFlag, yFlag) = DeconstructFlags();
        return (carryOut ? Flags.CF : Flags.None) |
               (equalOut ? Flags.EF : Flags.None) |
               (xFlag ? Flags.XF : Flags.None) |
               (yFlag ? Flags.YF : Flags.None);
    }

    private void StoreResult(Operation op, Nybble z, byte addr)
    {
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
                mem[addr] = z;
                break;
            case Dst.Flags:
                Flags = (Flags)(byte)z;
                break;
            case Dst.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(op.Dst));
        }
    }

    private void Jump(Operation op)
    {
        var (carryFlag, equalFlag, _, _) = DeconstructFlags();
        bool greaterThan = !carryFlag && !equalFlag;
        var doJmp = op.Cond switch
        {
            BranchCond.Never => false,
            BranchCond.Z => equalFlag,
            BranchCond.NZ => !equalFlag,
            BranchCond.C => carryFlag,
            BranchCond.NC => !carryFlag,
            BranchCond.GT => greaterThan,
            BranchCond.LE => !greaterThan,
            BranchCond.Always => true,
            _ => throw new ArgumentOutOfRangeException(nameof(op.Cond)),
        };
        if (doJmp)
        {
            PC = op.Jmp;
        }
    }

    private (bool carryFlag, bool equalFlag, bool xFlag, bool yFlag) DeconstructFlags()
    {
        bool carryFlag = (Flags & Flags.CF) != 0;
        bool equalFlag = (Flags & Flags.EF) != 0;
        bool xFlag = (Flags & Flags.XF) != 0;
        bool yFlag = (Flags & Flags.YF) != 0;
        return (carryFlag, equalFlag, xFlag, yFlag);
    }

    public void LoadProg(Stream stream, UInt16 instructionStart = 0, UInt16? instructionCount = null)
    {
        if (instructionStart >= Prog.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(instructionStart),
                "Start cannot extend past end of program memory");
        }

        if (instructionCount == null)
        {
            instructionCount = (UInt16)(Prog.Length - instructionStart);
        }
        else if (instructionCount.Value > Prog.Length - instructionStart)
        {
            throw new ArgumentOutOfRangeException(nameof(instructionStart),
                "Start + Count cannot extend past end of program memory");
        }

        var bytes = new byte[2 * instructionCount.Value];
        stream.ReadExactly(bytes, 0, instructionCount.Value * 2);

        for (int loc = 0; loc < instructionCount.Value; loc++)
        {
            Prog[instructionStart + loc] = Operation.FromUInt16(BitConverter.ToUInt16(bytes, loc * 2));
        }
    }
}