namespace Bit.Balette.Samples.Core;

/// <summary>
/// What the sample commands act on. Scoped like the palette's own registry - one per circuit on Interactive Server,
/// one per app on WebAssembly and in the MAUI app - so a command and the page that shows its effect read the same
/// instance.
/// </summary>
public sealed class SampleState
{
    /// <summary>Raised after every change, so the components that build commands from this state build them again.</summary>
    public event Action? Changed;

    /// <summary>The palette's and the page's colour theme: <c>auto</c>, <c>light</c> or <c>dark</c>.</summary>
    public string Theme { get; private set; } = "auto";

    public bool Compact { get; private set; }

    public int Count { get; private set; }

    public bool ShowDetails { get; private set; }

    public string ProjectName { get; private set; } = "Apollo";

    public string? Assignee { get; private set; }

    public IReadOnlyList<string> Labels { get; private set; } = [];

    /// <summary>What the last command did, shown at the bottom of every page.</summary>
    public string? LastAction { get; private set; }

    public void SetTheme(string theme)
    {
        Theme = theme;
        Record($"Theme set to {theme}");
    }

    public void ToggleCompact()
    {
        Compact = Compact is false;
        Record(Compact ? "Compact rows on" : "Compact rows off");
    }

    public void Increment()
    {
        Count++;
        Record($"Count incremented to {Count}");
    }

    public void Decrement()
    {
        if (Count == 0) return;

        Count--;
        Record($"Count decremented to {Count}");
    }

    public void ResetCount()
    {
        Count = 0;
        Record("Count reset");
    }

    public void ToggleDetails()
    {
        ShowDetails = ShowDetails is false;
        Record(ShowDetails ? "Details shown" : "Details hidden");
    }

    public void Rename(string name)
    {
        ProjectName = name;
        Record($"Project renamed to {name}");
    }

    public void Assign(string person)
    {
        Assignee = person;
        Record($"Assigned to {person}");
    }

    public void SetLabels(IEnumerable<string> labels)
    {
        Labels = [.. labels];
        Record(Labels.Count == 1 ? "1 label applied" : $"{Labels.Count} labels applied");
    }

    public void Record(string action)
    {
        LastAction = action;
        Changed?.Invoke();
    }
}
