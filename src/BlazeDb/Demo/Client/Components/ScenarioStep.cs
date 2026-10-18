namespace Bit.BlazeDb.Demo.Client.Components;

/// <summary>One line in a scenario trace, used by the interactive pages.</summary>
public sealed record ScenarioStep(int Number, string Title, string? Code = null, string? Note = null, string Tone = "");

/// <summary>Accumulates numbered steps while a scenario runs.</summary>
public sealed class ScenarioLog
{
    private readonly List<ScenarioStep> _steps = [];

    public IReadOnlyList<ScenarioStep> Steps => _steps;

    public void Clear() => _steps.Clear();

    public void Add(string title, string? code = null, string? note = null, string tone = "") =>
        _steps.Add(new ScenarioStep(_steps.Count + 1, title, code, note, tone));

    public void Ok(string title, string? code = null, string? note = null) => Add(title, code, note, "ok");

    public void Error(string title, string? code = null, string? note = null) => Add(title, code, note, "err");

    public void Run(string title, string? code = null, string? note = null) => Add(title, code, note, "run");
}
