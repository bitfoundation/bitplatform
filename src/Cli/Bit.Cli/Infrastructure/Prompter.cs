using Spectre.Console;

namespace Bit.Cli.Infrastructure;

public interface IPrompter
{
    bool CanPrompt { get; }

    string Text(string question, string? defaultValue, Func<string, string?>? validate = null);

    T Select<T>(string question, IReadOnlyList<T> choices, T defaultChoice, Func<T, string> display) where T : notnull;

    IReadOnlyList<T> MultiSelect<T>(string question, IReadOnlyList<T> choices, IEnumerable<T> selected, Func<T, string> display) where T : notnull;

    bool Confirm(string question, bool defaultValue);
}

public sealed class NonInteractivePrompter : IPrompter
{
    public bool CanPrompt => false;

    public string Text(string question, string? defaultValue, Func<string, string?>? validate = null)
        => defaultValue ?? throw new InvalidOperationException($"'{question}' needs an answer, and this session can't ask for one.");

    public T Select<T>(string question, IReadOnlyList<T> choices, T defaultChoice, Func<T, string> display) where T : notnull => defaultChoice;

    public IReadOnlyList<T> MultiSelect<T>(string question, IReadOnlyList<T> choices, IEnumerable<T> selected, Func<T, string> display) where T : notnull
        => [.. selected];

    public bool Confirm(string question, bool defaultValue) => defaultValue;
}

public sealed class SpectrePrompter(IAnsiConsole console) : IPrompter
{
    public bool CanPrompt => console.Profile.Capabilities.Interactive;

    public string Text(string question, string? defaultValue, Func<string, string?>? validate = null)
    {
        var prompt = new TextPrompt<string>($"[bold]{Markup.Escape(question)}[/]");

        if (defaultValue is not null)
        {
            prompt.DefaultValue(defaultValue);
            prompt.DefaultValueStyle(new Style(Color.Grey));
        }

        if (validate is not null)
        {
            prompt.Validate(answer => validate(answer) is { } error ? ValidationResult.Error($"[red]{Markup.Escape(error)}[/]") : ValidationResult.Success());
        }

        return console.Prompt(prompt).Trim();
    }

    public T Select<T>(string question, IReadOnlyList<T> choices, T defaultChoice, Func<T, string> display) where T : notnull
    {
        var ordered = choices.OrderByDescending(c => EqualityComparer<T>.Default.Equals(c, defaultChoice)).ToList();

        var prompt = new SelectionPrompt<T>()
            .Title($"[bold]{Markup.Escape(question)}[/]")
            .PageSize(Math.Max(3, Math.Min(12, ordered.Count)))
            .UseConverter(c => Markup.Escape(display(c)))
            .AddChoices(ordered);

        var answer = console.Prompt(prompt);
        console.MarkupLine($"[bold]{Markup.Escape(question)}[/] [blue]{Markup.Escape(display(answer))}[/]");
        return answer;
    }

    public IReadOnlyList<T> MultiSelect<T>(string question, IReadOnlyList<T> choices, IEnumerable<T> selected, Func<T, string> display) where T : notnull
    {
        if (choices.Count == 0)
            return [];

        var prompt = new MultiSelectionPrompt<T>()
            .Title($"[bold]{Markup.Escape(question)}[/]")
            .NotRequired()
            .PageSize(Math.Max(3, Math.Min(15, choices.Count + 1)))
            .InstructionsText("[grey](space to toggle, enter to accept)[/]")
            .UseConverter(c => Markup.Escape(display(c)))
            .AddChoices(choices);

        foreach (var item in selected)
        {
            prompt.Select(item);
        }

        var answer = console.Prompt(prompt);
        var summary = answer.Count == 0 ? "none" : string.Join(", ", answer.Select(display));
        console.MarkupLine($"[bold]{Markup.Escape(question)}[/] [blue]{Markup.Escape(summary)}[/]");
        return answer;
    }

    public bool Confirm(string question, bool defaultValue)
    {
        return console.Prompt(new ConfirmationPrompt($"[bold]{Markup.Escape(question)}[/]") { DefaultValue = defaultValue });
    }
}
