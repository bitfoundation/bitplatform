using Bit.Cli.Infrastructure;

namespace Bit.Cli.Tests.Infrastructure;

public sealed class ScriptedPrompter : IPrompter
{
    private readonly List<(string Question, object Answer)> answers = [];

    public List<Prompt> Prompts { get; } = [];

    public bool CanPrompt => true;

    public ScriptedPrompter On(string question, object answer)
    {
        answers.Add((question, answer));
        return this;
    }

    public Prompt? Find(string question) => Prompts.SingleOrDefault(p => p.Question.Contains(question, StringComparison.Ordinal));

    public string Text(string question, string? defaultValue, Func<string, string?>? validate = null)
    {
        Record(question, [], defaultValue is null ? [] : [defaultValue]);

        var answer = AnswerFor(question) as string ?? defaultValue ?? throw new InvalidOperationException($"Nothing answers '{question}'.");

        if (validate?.Invoke(answer) is { } error)
            throw new InvalidOperationException($"'{answer}' isn't accepted by '{question}': {error}");

        return answer;
    }

    public T Select<T>(string question, IReadOnlyList<T> choices, T defaultChoice, Func<T, string> display) where T : notnull
    {
        var labels = choices.Select(display).ToList();
        Record(question, labels, [display(defaultChoice)]);

        return AnswerFor(question) is string wanted ? choices[Match(labels, wanted)] : defaultChoice;
    }

    public IReadOnlyList<T> MultiSelect<T>(string question, IReadOnlyList<T> choices, IEnumerable<T> selected, Func<T, string> display) where T : notnull
    {
        var preselected = selected.ToList();
        var labels = choices.Select(display).ToList();
        Record(question, labels, preselected.Select(display));

        return AnswerFor(question) is string[] wanted ? [.. wanted.Select(w => choices[Match(labels, w)])] : preselected;
    }

    public bool Confirm(string question, bool defaultValue)
    {
        Record(question, [], [defaultValue ? "yes" : "no"]);

        return AnswerFor(question) is bool answer ? answer : defaultValue;
    }

    private object? AnswerFor(string question) => answers.LastOrDefault(a => question.Contains(a.Question, StringComparison.Ordinal)).Answer;

    private void Record(string question, IEnumerable<string> choices, IEnumerable<string> preselected) => Prompts.Add(new Prompt(question, [.. choices], [.. preselected]));

    private static int Match(List<string> labels, string wanted)
    {
        var exact = labels.FindIndex(l => string.Equals(l, wanted, StringComparison.OrdinalIgnoreCase));

        if (exact >= 0)
            return exact;

        var partial = labels.Select((label, index) => (label, index)).Where(l => l.label.Contains(wanted, StringComparison.OrdinalIgnoreCase)).ToList();

        return partial.Count == 1 ? partial[0].index : throw new InvalidOperationException($"'{wanted}' matches {partial.Count} of: {string.Join(" | ", labels)}");
    }

    public sealed record Prompt(string Question, IReadOnlyList<string> Choices, IReadOnlyList<string> Preselected);
}
