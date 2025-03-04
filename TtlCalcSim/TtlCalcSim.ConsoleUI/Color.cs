namespace TtlCalcSim.ConsoleUI;

public readonly record struct Color(ConsoleColor Bg, ConsoleColor Fg)
{
    public void Set()
    {
        Console.BackgroundColor = Bg;
        Console.ForegroundColor = Fg;
    }

    public static Color Get() => new(Console.BackgroundColor, Console.ForegroundColor);

    public static Color? None => null;

    // These need to be computed properties instead of statically-initialized fields
    // to work around https://github.com/dotnet/runtime/issues/104511
    public static Color FlagOff => new(ConsoleColor.DarkGray, ConsoleColor.Black);
    public static Color FlagOn => new(ConsoleColor.DarkGray, ConsoleColor.White);
    public static Color MemHL => new(ConsoleColor.DarkGray, ConsoleColor.White);
    public static Color Error => new(ConsoleColor.DarkGray, ConsoleColor.Red);
    public static Color DigitBright => new(ConsoleColor.Black, ConsoleColor.DarkRed);
    public static Color DigitDim => new(ConsoleColor.Black, ConsoleColor.DarkYellow);
    public static Color DigitDecayed => new(ConsoleColor.Black, ConsoleColor.DarkGray);
    public static Color Breakpoint => new(ConsoleColor.Black, ConsoleColor.Red);
}