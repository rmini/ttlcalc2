using System.Globalization;
using JetBrains.Annotations;

namespace TtlCalcSim.ConsoleUI;

public static class Program
{
    private static readonly Alu74181 Alu74181 = new();
    private static readonly Alu Alu = new(Alu74181);
    private static readonly InputOutput InputOutput = new();
    private static readonly Memory Mem = new();
    private static readonly TtlCalcSim Sim = new(Alu, Mem, InputOutput);

    private const int MultiplexedDigitLifeTicks = 100;

    private const byte IndBusy = 1;
    private const byte IndError = 2;
    private const byte IndNegE = 4;
    private const byte IndNegF = 8;

    private static readonly int[] DisplayDigitLives = new int[14];
    private static readonly byte[] DisplayDigits = new byte[14];
    private static byte _displayDp = 0;
    private static byte _displayIndicators = 0;
    private static byte _displayNxsel = 0;
    private static readonly bool[] Breakpoints = new bool[4096];
    private static bool _running = false;
    private static bool _breakOnJump = false;
    private static bool _ioHalt = false;
    private static bool _singleStep = false;
    private static (int Left, int Top)? _flagPos = null;

    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            using var fs = File.OpenRead(args[0]);
            Sim.LoadProg(fs);
        }

        InputOutput.AddOutputHandler(0, 0, (_, val) => _displayNxsel = val);
        InputOutput.AddOutputHandler(1, 1, (_, val) =>
        {
            if (_displayNxsel < DisplayDigits.Length)
            {
                DisplayDigitLives[_displayNxsel] = MultiplexedDigitLifeTicks;
                DisplayDigits[_displayNxsel] = val;
            }
        });
        InputOutput.AddOutputHandler(2, 2, (_, val) => _displayDp = val);
        InputOutput.AddOutputHandler(3, 3, (_, val) => _displayIndicators = val);

        Console.Clear();

        while (true)
        {
            DrawInterface();

            if (_breakOnJump && Sim.Prog[Sim.PC].Cond.HasValue)
                _running = false;
            if (_ioHalt && (Sim.Prog[Sim.PC].Src == Src.IO || Sim.Prog[Sim.PC].Dst == Dst.IO))
                _running = false;
            if (Breakpoints[Sim.PC])
                _running = false;

            if (Console.KeyAvailable || !_running)
            {
                _running = false;

                if (ProcessKeyCommand())
                    break;

                if (!_singleStep && !_running)
                    continue;
            }

            Sim.RunStep();
            DecayDisplayDigits();
            if (_singleStep)
                _singleStep = false;
        }
    }

    private static void DrawInterface()
    {
        DrawRegisters();
        DrawDisplay();
        DrawMemory();
        DrawProg();
        Console.SetCursorPosition(0, 21);
        Console.WriteLine(
            $"Ctrl: Quit|Step|Run|Halt|run to Jump|Toggle breakpoint|Go to|I/o halt({(_ioHalt ? "on" : "off")})");
        Console.WriteLine("      Program edit|memory Edit|Change register|Breakpoints|Load program");
        DoWithSavedCursorPosition(() =>
        {
            ClearLine();
            Console.WriteLine();
            ClearLine();
        });
        Console.WriteLine();
        Console.CursorTop = 23;
    }

    private static bool ProcessKeyCommand()
    {
        Console.TreatControlCAsInput = true;
        ConsoleKeyInfo key;
        try
        {
            key = Console.ReadKey(true);
        }
        finally
        {
            Console.TreatControlCAsInput = false;
        }

        if (key.Modifiers.HasFlag(ConsoleModifiers.Control))
        {
            if (key.Key == ConsoleKey.Q)
            {
                return true;
            }

            switch (key.Key)
            {
                case ConsoleKey.S:
                    _singleStep = true;
                    break;
                case ConsoleKey.R:
                    _running = true;
                    break;
                case ConsoleKey.H:
                    break;
                case ConsoleKey.J:
                    _running = true;
                    _breakOnJump = true;
                    break;
                case ConsoleKey.T:
                    Breakpoints[Sim.PC] = !Breakpoints[Sim.PC];
                    break;
                case ConsoleKey.G:
                    CommandGoTo();
                    break;
                case ConsoleKey.I:
                    _ioHalt = !_ioHalt;
                    break;
                case ConsoleKey.P:
                    CommandProgramEdit();
                    break;
                case ConsoleKey.E:
                    CommandMemoryEdit();
                    break;
                case ConsoleKey.C:
                    CommandChangeRegister();
                    break;
                case ConsoleKey.L:
                    CommandLoadProgram();
                    break;
                default:
                    Console.Beep();
                    break;
            }
        }

        return false;
    }

    private static void CommandLoadProgram()
    {
        var file = ReadWithPrompt("Load program: ");
        if (string.IsNullOrWhiteSpace(file))
            return;
        if (!File.Exists(file))
        {
            WriteError($"{file} (File not found)");
            return;
        }

        try
        {
            using var fs = File.OpenRead(file);
            Sim.LoadProg(fs);
        }
        catch (Exception e)
        {
            WriteError(e.Message);
        }
    }

    private static void CommandChangeRegister()
    {
        Console.Write("Change register (X, Y, H, L, Flags): ");
        var regKey = Console.ReadKey(true);
        ClearLine();
        Console.CursorLeft = 0;
        switch (regKey.Key)
        {
            case ConsoleKey.X:
            case ConsoleKey.Y:
            case ConsoleKey.H:
            case ConsoleKey.L:
            {
                var strVal = ReadWithPrompt($"Value for {char.ToUpper(regKey.KeyChar)}: ");
                if (!TryParseValue(strVal, out var val) || val > 0xF)
                {
                    WriteError(strVal);
                    return;
                }

                switch (regKey.Key)
                {
                    case ConsoleKey.X:
                        Sim.X = (byte)val;
                        break;
                    case ConsoleKey.Y:
                        Sim.Y = (byte)val;
                        break;
                    case ConsoleKey.H:
                        Sim.H = (byte)val;
                        break;
                    case ConsoleKey.L:
                        Sim.L = (byte)val;
                        break;
                }

                break;
            }
            case ConsoleKey.F:
                Console.Write("Toggle flag (C, E, X, Y), Enter to continue: ");
                ConsoleKeyInfo flagKey;
                do
                {
                    flagKey = Console.ReadKey(true);
                    switch (flagKey.Key)
                    {
                        case ConsoleKey.C:
                            Sim.Flags ^= Flags.CF;
                            break;
                        case ConsoleKey.E:
                            Sim.Flags ^= Flags.EF;
                            break;
                        case ConsoleKey.X:
                            Sim.Flags ^= Flags.XF;
                            break;
                        case ConsoleKey.Y:
                            Sim.Flags ^= Flags.YF;
                            break;
                        case ConsoleKey.Enter:
                            break;
                        default:
                            Console.Beep();
                            continue;
                    }

                    DoWithSavedCursorPosition(DrawFlags);
                } while (flagKey.Key != ConsoleKey.Enter);

                break;
            default:
                Console.Beep();
                return;
        }
    }

    private static void ClearLine()
    {
        DoWithSavedCursorPosition(() =>
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.BufferWidth));
        });
    }

    private static void CommandMemoryEdit()
    {
        Console.Write("Memory edit: ");
        var strLoc = ReadWithPrompt("Location: ");
        if (!TryParseValue(strLoc, out var loc) || loc > byte.MaxValue)
        {
            WriteError(strLoc);
            return;
        }

        var strVal = ReadWithPrompt("Value: ");
        if (!TryParseValue(strVal, out var val) || val > 0xF)
        {
            WriteError(strVal);
            return;
        }

        Mem[(byte)loc] = (byte)val;
    }

    private static void CommandProgramEdit()
    {
        Console.Write("Program edit: ");
        var strLoc = ReadWithPrompt("Location: ");
        if (!TryParseValue(strLoc, out var loc) || loc > 0xfff)
        {
            WriteError(strLoc);
            return;
        }

        var strVal = ReadWithPrompt("Value: ");
        if (!TryParseValue(strVal, out var val) || val > UInt16.MaxValue)
        {
            WriteError(strVal);
            return;
        }

        Sim.Prog[loc] = Operation.FromUInt16((UInt16)val);
    }

    private static void CommandGoTo()
    {
        var addr = ReadWithPrompt("Go to: ");
        if (!TryParseValue(addr, out var gotoAddr) || gotoAddr > UInt16.MaxValue)
        {
            WriteError(addr);
            return;
        }

        Sim.PC = (UInt16)gotoAddr;
    }

    private static string ReadWithPrompt(string prompt)
    {
        Console.Write(prompt);
        string s = "";
        DoWithSavedCursorPosition(() => { s = Console.ReadLine() ?? ""; });
        Console.CursorLeft += s.Length + 2;
        return s;
    }

    private static void DecayDisplayDigits()
    {
        for (var i = 0; i < DisplayDigitLives.Length; i++)
        {
            if (DisplayDigitLives[i] > 0)
            {
                DisplayDigitLives[i]--;
            }
        }
    }

    private static void WriteError(string strVal)
    {
        Console.SetCursorPosition(0, 24);
        WriteWithColor($"Bad value: {strVal}; press a key", Color.Error);
        Console.ReadKey();
    }

    private static bool TryParseValue(string s, out uint val)
    {
        if (s.StartsWith("0x"))
            return uint.TryParse(s[2..], NumberStyles.HexNumber, null, out val);
        if (s.StartsWith("0b"))
            return uint.TryParse(s[2..], NumberStyles.HexNumber, null, out val);
        return uint.TryParse(s, out val);
    }

    private static void DrawDisplay()
    {
        Console.SetCursorPosition(0, 1);

        Console.Write("Display: "); //"-0.0.0.0.0.0.0.0.0.0.0.0.  -00   Error o  Busy o");
        WriteWithColor((_displayIndicators & IndNegF) != 0 ? "-" : " ", Color.DigitBright);
        for (var i = 0; i < DisplayDigits.Length; i++)
        {
            var digit = DisplayDigits[i];
            WriteWithColor(digit <= 9 ? $"{digit}" : " ", GetDigitColor(i));

            if (i < 12)
                WriteWithColor(_displayDp == i ? "." : " ", Color.DigitBright);
            if (i == 11)
            {
                Console.Write("  ");
                WriteWithColor((_displayIndicators & IndNegE) != 0 ? "-" : " ", Color.DigitBright);
            }
        }

        Console.Write("   Error ");
        var error = (_displayIndicators & IndError) != 0;
        WriteWithColor(error ? "*" : "o", error ? Color.DigitBright : Color.DigitDecayed);
        Console.Write("  Busy ");
        var busy = (_displayIndicators & IndBusy) != 0;
        WriteWithColor(busy ? "*" : "o", busy ? Color.DigitBright : Color.DigitDecayed);
    }

    private static Color? GetDigitColor(int i)
    {
        var life = DisplayDigitLives[i];
        return life switch
        {
            > MultiplexedDigitLifeTicks / 2 => Color.DigitBright,
            > 0 => Color.DigitDim,
            _ => Color.DigitDecayed
        };
    }

    public static void DrawProg()
    {
        Console.SetCursorPosition(0, 3);
        Console.Write("Program:");
        var start = Sim.PC > 8 ? Sim.PC - 8 : 0;
        for (var i = 0; i < 16; i++)
        {
            Console.SetCursorPosition(0, 4 + i);
            var pc = (start + i) & 0xfff;
            WriteWithColor(Breakpoints[pc] ? "*" : " ", Color.Breakpoint);
            Console.Write($"{(pc == Sim.PC ? ">" : " ")} {pc:X03}: ");
            Console.Write(Sim.Prog[pc].Disassemble().PadRight(40));
        }
    }

    public static void DrawMemory()
    {
        Console.SetCursorPosition(49, 2);
        Console.Write("Memory:");
        Console.SetCursorPosition(49, 3);
        Console.Write("    ");
        WriteWithSpaces(16, 4, j => Console.Write($"{j:X1}"));

        for (var i = 0; i < 16; i++)
        {
            Console.SetCursorPosition(49, 4 + i);
            Console.Write($"{i:X1}0: ");
            WriteWithSpaces(16, 4, j =>
            {
                var color = (i * 16 + j == Sim.HL) ? Color.MemHL : Color.None;
                WriteWithColor($"{Mem[(byte)(i * 16 + j)]:X1}", color);
            });
        }
    }

    public static void DrawRegisters()
    {
        Console.SetCursorPosition(0, 0);
        Console.Write($"PC: {Sim.PC:X03}  X: {Sim.X:X01}  Y: {Sim.Y:X01}  HL: {Sim.HL:X02}  Flags: ");

        _flagPos ??= Console.GetCursorPosition();
        DrawFlags();
    }

    private static void DrawFlags()
    {
        if (_flagPos != null)
            Console.SetCursorPosition(_flagPos.Value.Left, _flagPos.Value.Top);
        WriteWithSpaces(4, 1, i => WriteFlag((Flags)(1 << i)));
    }

    private static void DoWithSavedCursorPosition([InstantHandle] Action action)
    {
        var (savedLeft, savedTop) = Console.GetCursorPosition();
        try
        {
            action();
        }
        finally
        {
            Console.SetCursorPosition(savedLeft, savedTop);
        }
    }

    private static void WriteFlag(Flags flag)
    {
        WriteWithColor(" " + flag + " ", Sim.Flags.HasFlag(flag) ? Color.FlagOn : Color.FlagOff);
    }

    private static void WriteWithColor(string text, Color? color = null)
    {
        Color? origColor = null;

        if (color.HasValue)
        {
            origColor = Color.Get();
            color.Value.Set();
        }

        Console.Write(text);
        origColor?.Set();
    }

    public static void WriteWithSpaces(int count, int spacing, [InstantHandle] Action<int> write)
    {
        for (var i = 0; i < count; i++)
        {
            write(i);
            if (i % spacing == spacing - 1)
            {
                Console.Write(" ");
            }
        }
    }
}