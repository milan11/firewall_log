namespace FirewallLog.Utils;

public static class ConsoleUtils
{
    public static void WriteWithColor(ConsoleColor color, string str)
    {
        var origColor = Console.ForegroundColor;
        Console.ForegroundColor = color;

        Console.Write(str);

        Console.ForegroundColor = origColor;
    }
}