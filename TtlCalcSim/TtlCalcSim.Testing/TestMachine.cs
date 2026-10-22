namespace TtlCalcSim.Testing;

/// <summary>
/// A TtlCalc2 simulator loaded with an <see cref="AsmProgram"/>, with helpers for setting up
/// state, running to labels, and inspecting results in tests.
/// </summary>
public class TestMachine
{
    public const long DefaultMaxSteps = 1_000_000;
    public const int DefaultTraceLength = 16;

    private readonly Memory _mem = new();
    private readonly Queue<TraceEntry> _trace = new();

    public TestMachine(AsmProgram program, int traceLength = DefaultTraceLength)
    {
        Program = program;
        TraceLength = traceLength;
        IO = new TestInputOutput(() => Steps);
        Sim = new TtlCalcSim(new Alu(new Alu74181()), _mem, IO);
        Mem = new MemoryAccessor(this);

        for (var i = 0; i < program.Words.Count && i < Sim.Prog.Length; i++)
            Sim.Prog[i] = Operation.FromUInt16(program.Words[i]);
    }

    public AsmProgram Program { get; }

    /// <summary>The underlying simulator, for anything the helpers don't cover.</summary>
    public TtlCalcSim Sim { get; }

    public TestInputOutput IO { get; }

    /// <summary>Data memory, addressed by number or data label.</summary>
    public MemoryAccessor Mem { get; }

    /// <summary>Number of instructions executed so far.</summary>
    public long Steps { get; private set; }

    /// <summary>Number of recent instructions kept for failure messages.</summary>
    public int TraceLength { get; }

    /// <summary>The most recently executed instructions, oldest first.</summary>
    public IReadOnlyList<TraceEntry> Trace => _trace.ToList();

    public IReadOnlyList<IoWrite> IoWrites => IO.Writes;

    public byte X { get => Sim.X; set => Sim.X = value; }
    public byte Y { get => Sim.Y; set => Sim.Y = value; }
    public byte H { get => Sim.H; set => Sim.H = value; }
    public byte L { get => Sim.L; set => Sim.L = value; }
    public byte HL { get => Sim.HL; set => Sim.HL = value; }
    public Flags Flags { get => Sim.Flags; set => Sim.Flags = value; }
    public int PC { get => Sim.PC; set => Sim.PC = value; }

    /// <summary>Sets the PC to a program label.</summary>
    public TestMachine StartAt(string label)
    {
        PC = Program[label];
        return this;
    }

    public void QueueInput(byte addr, params byte[] values) => IO.QueueInput(addr, values);

    public void OnInput(byte addr, Func<byte> handler) => IO.OnInput(addr, handler);

    /// <summary>Executes one instruction.</summary>
    public void Step()
    {
        if (TraceLength > 0)
        {
            if (_trace.Count == TraceLength)
                _trace.Dequeue();
            _trace.Enqueue(new TraceEntry(Steps + 1, PC, Sim.Prog[PC]));
        }

        Steps++;
        Sim.RunStep();
    }

    /// <summary>
    /// Executes instructions until <paramref name="condition"/> holds after a step. At least one
    /// instruction is always executed, so running from a label until the same label runs one
    /// full loop iteration. Returns the number of instructions executed.
    /// </summary>
    /// <exception cref="ProgramRunException">
    /// The step limit was reached, or execution ran past the end of the program first.
    /// </exception>
    public long RunUntil(Func<TestMachine, bool> condition, long maxSteps = DefaultMaxSteps, string? description = null)
    {
        description ??= "condition";
        var start = Steps;
        while (true)
        {
            if (Steps - start >= maxSteps)
                throw new ProgramRunException(
                    $"Did not reach {description} within {maxSteps} steps (PC=0x{PC:X3})", Trace);

            Step();

            if (condition(this))
                return Steps - start;

            if (PC >= Program.End)
                throw new ProgramRunException(
                    $"Ran past the end of the program (0x{Program.End:X3}) before reaching {description}", Trace);
        }
    }

    /// <summary>Runs until the PC reaches <paramref name="addr"/>.</summary>
    public long RunUntil(int addr, long maxSteps = DefaultMaxSteps) =>
        RunUntil(m => m.PC == addr, maxSteps, $"0x{addr:X3}");

    /// <summary>Runs until the PC reaches a program label.</summary>
    public long RunUntil(string label, long maxSteps = DefaultMaxSteps)
    {
        var addr = Program[label];
        return RunUntil(m => m.PC == addr, maxSteps, $"{label} (0x{addr:X3})");
    }

    /// <summary>Runs until execution passes the last instruction of the program.</summary>
    public long RunToEnd(long maxSteps = DefaultMaxSteps) =>
        RunUntil(m => m.PC >= m.Program.End, maxSteps, $"the end of the program (0x{Program.End:X3})");

    /// <summary>
    /// Reads a multi-digit number stored one digit per nybble, least significant digit first.
    /// </summary>
    public long ReadNumber(int addr, int digits, int radix = 10)
    {
        long value = 0;
        for (var i = digits - 1; i >= 0; i--)
        {
            var digit = Mem[addr + i];
            if (digit >= radix)
                throw new InvalidOperationException(
                    $"Mem[0x{addr + i:X2}] = 0x{digit:X} is not a valid base-{radix} digit");
            value = value * radix + digit;
        }

        return value;
    }

    public long ReadNumber(string label, int digits, int radix = 10) => ReadNumber(Program[label], digits, radix);

    /// <summary>
    /// Writes a multi-digit number one digit per nybble, least significant digit first.
    /// </summary>
    public void WriteNumber(int addr, int digits, long value, int radix = 10)
    {
        for (var i = 0; i < digits; i++)
        {
            Mem[addr + i] = (byte)(value % radix);
            value /= radix;
        }

        if (value != 0)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value does not fit in {digits} base-{radix} digits");
    }

    public void WriteNumber(string label, int digits, long value, int radix = 10) =>
        WriteNumber(Program[label], digits, value, radix);

    public sealed class MemoryAccessor(TestMachine machine)
    {
        public byte this[int addr]
        {
            get => machine._mem[checked((byte)addr)];
            set => machine._mem[checked((byte)addr)] = value;
        }

        public byte this[string label]
        {
            get => this[machine.Program[label]];
            set => this[machine.Program[label]] = value;
        }

        /// <summary>Reads <paramref name="count"/> consecutive nybbles.</summary>
        public byte[] Read(int addr, int count) => Enumerable.Range(addr, count).Select(a => this[a]).ToArray();

        /// <summary>Writes consecutive nybbles starting at <paramref name="addr"/>.</summary>
        public void Write(int addr, params byte[] values)
        {
            for (var i = 0; i < values.Length; i++)
                this[addr + i] = values[i];
        }
    }
}
