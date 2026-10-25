using System.Text;

namespace Bit.Cli.Infrastructure;

public sealed class CliLog : IDisposable
{
    private const int KeptLogFiles = 30;

    private readonly Lock gate = new();
    private readonly string? directory;
    private readonly string name;
    private StreamWriter? writer;

    public CliLog(string? directory, string name)
    {
        this.directory = directory;
        this.name = name;
    }

    public static CliLog None { get; } = new(null, "none");

    public string? FilePath { get; private set; }

    public void Write(string line)
    {
        if (directory is null)
            return;

        lock (gate)
        {
            try
            {
                writer ??= Open();
                writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {line}");
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            writer?.Dispose();
            writer = null;
        }
    }

    private StreamWriter Open()
    {
        Directory.CreateDirectory(directory!);
        RemoveOldLogs();

        FilePath = Path.Combine(directory!, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log");
        return new StreamWriter(new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
    }

    private void RemoveOldLogs()
    {
        try
        {
            foreach (var oldLog in new DirectoryInfo(directory!).GetFiles("*.log").OrderByDescending(f => f.LastWriteTimeUtc).Skip(KeptLogFiles))
            {
                oldLog.Delete();
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
