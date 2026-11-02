using Spectre.Console;

namespace Bit.Cli.Infrastructure;

public sealed class ConsoleTitle : IDisposable
{
    private readonly Lock gate = new();
    private readonly bool enabled;
    private readonly Spinner spinner;
    private readonly string? original;
    private string idle;
    private Timer? timer;
    private int generation;

    public ConsoleTitle(bool enabled, bool unicode, string idle)
    {
        this.enabled = enabled;
        this.idle = idle;
        spinner = unicode ? Spinner.Known.Dots : Spinner.Known.Line;

        if (enabled is false)
            return;

        if (OperatingSystem.IsWindows())
        {
            original = ReadWindowsTitle();
        }
        else
        {
            Write("\u001b[22;0t");
        }

        Apply(idle);
    }

    public static ConsoleTitle None { get; } = new(false, false, "");

    public void Working(string activity)
    {
        if (enabled is false)
            return;

        var text = Clean(activity);

        lock (gate)
        {
            timer?.Dispose();
            var mine = ++generation;
            var frame = 0;

            timer = new Timer(_ =>
            {
                lock (gate)
                {
                    if (mine == generation)
                    {
                        Apply($"{spinner.Frames[frame++ % spinner.Frames.Count]} {text}");
                    }
                }
            }, null, TimeSpan.Zero, spinner.Interval);
        }
    }

    public void Idle() => Settle(null);

    public void Settle(string? text)
    {
        if (enabled is false)
            return;

        lock (gate)
        {
            StopSpinner();

            if (text is not null)
            {
                idle = Clean(text);
            }

            Apply(idle);
        }
    }

    public void Dispose()
    {
        if (enabled is false)
            return;

        lock (gate)
        {
            StopSpinner();

            if (OperatingSystem.IsWindows())
            {
                if (original is not null)
                {
                    Apply(original);
                }
            }
            else
            {
                Write("\u001b[23;0t");
            }
        }
    }

    private void StopSpinner()
    {
        timer?.Dispose();
        timer = null;
        generation++;
    }

    private static string Clean(string text) => new([.. text.Where(c => char.IsControl(c) is false)]);

    private static void Apply(string title)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                Console.Title = title;
            }
            catch (Exception exp) when (exp is IOException or PlatformNotSupportedException or ArgumentException)
            {
            }
        }
        else
        {
            Write($"\u001b]0;{title}\u0007");
        }
    }

    private static string? ReadWindowsTitle()
    {
        try
        {
            return OperatingSystem.IsWindows() ? Console.Title : null;
        }
        catch (Exception exp) when (exp is IOException or PlatformNotSupportedException)
        {
            return null;
        }
    }

    private static void Write(string sequence)
    {
        try
        {
            Console.Out.Write(sequence);
            Console.Out.Flush();
        }
        catch (IOException)
        {
        }
    }
}
