using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;

namespace BetterBalance;

internal static class BBLog
{
    private static string? Prefix;

    internal static void Log(int purpose, string message)
    {
        switch (purpose)
        {
            case 0: LogToConsole(ConsoleColor.Magenta, $"{Prefix} -> " + message); break;
            case 1: LogToConsole(ConsoleColor.Green, $"{Prefix} -> " + message); break;
            case 2: LogToConsole(ConsoleColor.Red, $"{Prefix} -> " + message); break;
        }
    }

    internal static void LogToChat(CCSPlayerController? player, string messageToLog)
    {
        player?.PrintToChat($" {ChatColors.Red}{Prefix} {messageToLog.ReplaceColorTags()}");
    }

    internal static void LogToChatAll(string messageToLog)
    {
        Server.PrintToChatAll($" {ChatColors.Red}{Prefix} {messageToLog.ReplaceColorTags()}");
    }

    internal static void LogToConsole(ConsoleColor color, string messageToLog)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(messageToLog);
        Console.ResetColor();
    }

    internal static void SetPrefix(string prefix)
    {
        Prefix = prefix;
    }
}