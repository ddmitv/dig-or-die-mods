
using System;
using System.IO;
using System.Text.RegularExpressions;

public static class Logging {
    private static readonly StreamWriter _logFile;

    static Logging() {
        string logPath = GetLogPath();
        _logFile = new StreamWriter(logPath);
        Info($"Logging to \"{Path.GetFullPath(logPath)}\"");
    }
    private static string GetLogPath() {
        string template = Server.Config.LogPath;
        string result = template;
        DateTime date = DateTime.Now;
        foreach (Match match in Regex.Matches(template, @"\{([^}]+)\}")) {
            result = result.Replace(match.Value, date.ToString(match.Groups[1].Value));
        }
        Directory.CreateDirectory(Path.GetDirectoryName(result)!);
        return result;
    }

    private static void LogMsg(string msg, ConsoleColor color) {
        var prevForegroundColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(msg);
        Console.ForegroundColor = prevForegroundColor;

        _logFile.WriteLine($"{GetLoggingTime()}: {msg}");
        _logFile.Flush();
    }
    private static string GetLoggingTime() {
        return $"<{DateTime.Now:HH:mm:ss}>";
    }

    public static void Error(string msg) {
        LogMsg($"[ERROR] {msg}", ConsoleColor.Red);
    }
    public static void Warning(string msg) {
        LogMsg($"[WARNING] {msg}", ConsoleColor.Yellow);
    }
    public static void Info(string msg) {
        LogMsg($"[INFO] {msg}", ConsoleColor.Blue);
    }
    public static void Debug(string msg) {
        LogMsg($"[DEBUG] {msg}", ConsoleColor.White);
    }
    public static void Chat(string msg) {
        LogMsg($"[CHAT] {msg}", ConsoleColor.Cyan);
    }
}
