namespace TtlCalcSim;

public class InputOutput
{
    public virtual Nybble Read(byte addr)
    {
        return 0;
    }

    public virtual void Write(byte addr, Nybble d)
    {
    }
}