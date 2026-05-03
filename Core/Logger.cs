using Discord;
using DragonBot;

namespace DragonBot.Core
{
    public static class Logger
    {
        private static readonly StreamWriter Writer = new(Path.Combine(Program.Settings!.LogDirectory, "latest.log"));
        public static async Task Log(LogMessage logMessage)
        {
            await WriteLog($"[{DateTime.Now}] {logMessage.Severity}: {logMessage.Message}");
            await Task.CompletedTask;
        }
        public static async Task Log(string message, LogSeverity severity)
        {
            await WriteLog($"[{DateTime.Now}] {severity}: {message}");
            await Task.CompletedTask;
        }
        private static async Task WriteLog(string message)
        {
            Directory.CreateDirectory(Program.Settings!.LogDirectory);
            await Writer.WriteAsync(message);
            Writer.Flush();
            await Task.CompletedTask;
        }
    }
}