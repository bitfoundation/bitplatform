using Spectre.Console;

namespace Bit.Cli.Infrastructure;

public enum StepStatus
{
    Succeeded,
    Warning,
    Failed,
    Skipped
}

public sealed class CliConsole(IAnsiConsole output, IAnsiConsole error)
{
    private const int TitleWidth = 34;

    public IAnsiConsole Out { get; } = output;

    public IAnsiConsole Error { get; } = error;

    public bool Unicode => Out.Profile.Capabilities.Unicode;

    public bool IsInteractive => Out.Profile.Capabilities.Interactive;

    public int Width => Math.Clamp(Out.Profile.Width, 60, 110);

    public string OkSymbol => Unicode ? "✓" : "+";

    public string FailSymbol => Unicode ? "✗" : "x";

    public string WarnSymbol => "!";

    public string SkipSymbol => "-";

    public string BulletSymbol => Unicode ? "◆" : "*";

    public static CliConsole Create()
    {
        return new CliConsole(
            AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Out) }),
            AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) }));
    }

    public void Line(string markup = "") => Out.MarkupLine(markup);

    public void Text(string text) => Out.WriteLine(text);

    public void Dim(string text) => Out.MarkupLine($"[grey]{Markup.Escape(text)}[/]");

    public void Warn(string text) => Out.MarkupLine($"[yellow]{WarnSymbol}[/] {Markup.Escape(text)}");

    public void StepWarning(string text, string? nextLine = null)
    {
        var grid = new Grid { Width = Width }
            .AddColumn(new GridColumn().NoWrap().PadLeft(2).PadRight(1))
            .AddColumn(new GridColumn().PadLeft(0).PadRight(0));

        grid.AddRow(new Markup($"[yellow]{WarnSymbol}[/]"), new Markup(nextLine is null ? Markup.Escape(text) : $"{Markup.Escape(text)}\n{Markup.Escape(nextLine)}"));
        Out.Write(grid);
    }

    public void Fail(string text) => Error.MarkupLine($"[red]{FailSymbol}[/] {Markup.Escape(text)}");

    public void Step(StepStatus status, string title, string? detail = null, TimeSpan? duration = null)
    {
        var (symbol, color) = status switch
        {
            StepStatus.Succeeded => (OkSymbol, "green"),
            StepStatus.Warning => (WarnSymbol, "yellow"),
            StepStatus.Failed => (FailSymbol, "red"),
            _ => (SkipSymbol, "grey")
        };

        var durationText = duration is { } d ? FormatDuration(d) : "";
        var titlePart = title.Length < TitleWidth ? title.PadRight(TitleWidth) : title + " ";
        var remaining = Width - 4 - titlePart.Length;
        var shownDetail = Truncate(detail ?? "", Math.Max(0, remaining - durationText.Length - 2));
        var spaces = new string(' ', Math.Max(1, remaining - shownDetail.Length - durationText.Length));
        var titleMarkup = status is StepStatus.Skipped ? $"[grey]{Markup.Escape(titlePart)}[/]" : Markup.Escape(titlePart);

        Out.MarkupLine($"  [{color}]{symbol}[/] {titleMarkup}[grey]{Markup.Escape(shownDetail)}{spaces}{durationText}[/]");
    }

    public async Task<T> RunWithStatusAsync<T>(string title, Func<Action<string>, Task<T>> work)
    {
        if (IsInteractive is false)
            return await work(_ => { });

        T result = default!;

        await Out.Status()
            .Spinner(Unicode ? Spinner.Known.Dots : Spinner.Known.Line)
            .SpinnerStyle(new Style(Color.Blue))
            .StartAsync(Markup.Escape(title), async context =>
            {
                result = await work(line =>
                {
                    var text = Truncate(line.Trim(), Math.Max(10, Width - title.Length - 10));
                    context.Status($"{Markup.Escape(title)} [grey]{Markup.Escape(text)}[/]");
                });
            });

        return result;
    }

    public static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 60
            ? $"{duration.TotalSeconds:0.0}s"
            : $"{(int)duration.TotalMinutes}m {duration.Seconds:00}s";
    }

    public static string Truncate(string text, int length)
    {
        if (length <= 0)
            return "";

        return text.Length <= length ? text : length <= 3 ? text[..length] : text[..(length - 3)] + "...";
    }
}
