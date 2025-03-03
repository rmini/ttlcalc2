namespace TtlCalcSim;

public class Memory
{
    protected readonly Nybble[] Mem = new Nybble[256];

    public Nybble this[byte addr]
    {
        get => Get(addr);
        set => Set(addr, value);
    }

    protected virtual Nybble Get(byte addr) => Mem[addr];
    protected virtual void Set(byte addr, Nybble value) => Mem[addr] = value;
}