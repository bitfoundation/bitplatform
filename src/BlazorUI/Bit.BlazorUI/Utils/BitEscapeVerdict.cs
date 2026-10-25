namespace Bit.BlazorUI;

/// <summary>
/// What the browser answered for the Escape a component's .NET keydown handler is about to hear: whether the press is
/// somebody else's - a surface nested inside the component, a part of it that claimed the key first - rather than the
/// component's own. It arrives through the component's <c>OnEscapeVerdict</c> callback, which hands it to
/// <see cref="Set"/>, ahead of the keydown Blazor dispatches right after it (Utils.sendEscapeVerdicts), and the handler
/// takes it before deciding anything (<see cref="Take"/>), so it never outlives the press it was given for. The browser
/// only answers the presses that are not the component's own, and the one right after them, so an answer that is never
/// taken - its keydown was stopped on the way to the handler - is overwritten before the next one is read.
/// </summary>
internal sealed class BitEscapeVerdict
{
    private bool _foreign;

    /// <summary>What the browser answered for the Escape the key handler is about to hear.</summary>
    public void Set(bool foreign) => _foreign = foreign;

    /// <summary>Whether the press the key handler is hearing is somebody else's. Read once.</summary>
    public bool Take()
    {
        var foreign = _foreign;
        _foreign = false;
        return foreign;
    }

    /// <summary>Forgets an answer the watch that sent it no longer stands behind.</summary>
    public void Reset() => _foreign = false;
}
