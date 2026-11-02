using System.Diagnostics;
using System.IO;

namespace Clipline.Spike;

internal static class Log
{
    private static readonly object Gate = new();

    public static string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clipline-spike.log");

    public static void Reset()
    {
        Write($"=== spike run started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
        Write($"log file: {Path}");
    }

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        Debug.WriteLine(line);
        lock (Gate)
        {
            // 辅助进程与本进程共用同一个日志文件，偶尔会撞独占，重试即可。
            for (var attempt = 1; attempt <= 4; attempt++)
            {
                try
                {
                    using var stream = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    using var writer = new StreamWriter(stream, System.Text.Encoding.UTF8);
                    writer.WriteLine(line);
                    return;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(25);
                }
                catch (Exception)
                {
                    return;
                }
            }
        }
    }
}
