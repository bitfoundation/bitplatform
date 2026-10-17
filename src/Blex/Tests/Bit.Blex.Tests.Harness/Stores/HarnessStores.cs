namespace Bit.Blex.Tests.Harness.Stores;

/// <summary>Persisted to localStorage and recorded by the history: the store the persistence, cross-tab and undo tests drive.</summary>
[BlexStore(Name = "counter", Persist = true)]
public partial class CounterStore
{
    [BlexState] private int _count;

    [BlexAction] private void OnIncrement() => Count++;

    [BlexAction] private void OnReset() => Count = 0;
}

/// <summary>
/// Not persisted: its only way from the prerender pass to the interactive render is Blazor's
/// PersistentComponentState, which is exactly what the handoff test proves. A token issued while
/// prerendering that is still on screen once the page is interactive was handed over; one replaced by a
/// new token was rendered twice.
/// </summary>
[BlexStore(Name = "token")]
public partial class TokenStore
{
    [BlexState] private string _token = "";

    [BlexState] private string _issuedOn = "";

    [BlexAction] private void OnIssue(string token, string issuedOn)
    {
        Token = token;
        IssuedOn = issuedOn;
    }
}

/// <summary>Two independent fields, so a selector subscription can be shown ignoring the one it does not read.</summary>
[BlexStore(Name = "profile")]
public partial class ProfileStore
{
    [BlexState] private string _displayName = "ada";

    [BlexState] private int _visits;

    [BlexAction] private void OnRename(string displayName) => DisplayName = displayName;

    [BlexAction] private void OnVisit() => Visits++;
}

/// <summary>An effect with Latest concurrency: a second run cancels the first, and Cancel stops it outright.</summary>
[BlexStore(Name = "lab")]
public partial class EffectStore
{
    [BlexState] private string _result = "";

    [BlexEffect(Concurrency = BlexEffectConcurrency.Latest)]
    private async Task OnLoad(int delayMilliseconds, CancellationToken cancellationToken)
    {
        await Task.Delay(delayMilliseconds, cancellationToken);

        Result = $"loaded after {delayMilliseconds}ms";
    }
}
