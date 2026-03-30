using System.IO;
using System.Text;

namespace OpenClawClient.Core.Services;

/// <summary>
/// 客户端日志服务 - 用于调试连接问题
/// </summary>
public static class ClientLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenClawClient", "Logs");
    
    private static readonly string LogFile = Path.Combine(LogDirectory, $"client_{DateTime.Now:yyyyMMdd}.log");
    private static readonly object _lock = new();

    static ClientLogger()
    {
        Directory.CreateDirectory(LogDirectory);
    }

    public static void LogInfo(string message)
    {
        Log("INFO", message);
    }

    public static void LogWarning(string message)
    {
        Log("WARN", message);
    }

    public static void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
        {
            message += $" | Exception: {ex.Message} | StackTrace: {ex.StackTrace}";
        }
        Log("ERROR", message);
    }

    public static void LogDebug(string message)
    {
        Log("DEBUG", message);
    }

    private static void Log(string level, string message)
    {
        try
        {
            lock (_lock)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}{Environment.NewLine}";
                
                File.AppendAllText(LogFile, logEntry, Encoding.UTF8);
                
                // 同时输出到控制台（如果可用）
                Console.WriteLine($"[ClientLog] {logEntry.Trim()}");
            }
        }
        catch (Exception ex)
        {
            // 避免日志记录本身导致异常
            Console.WriteLine($"[ClientLog Error] Failed to write log: {ex.Message}");
        }
    }

    public static string GetLogFilePath()
    {
        return LogFile;
    }
}