namespace Bit.Bheadless;

/// <summary>
/// The focus moves a component decides on itself and asks the browser for: a calendar's arrow keys (a move can show
/// another month, which only the component can render) and a date field's segments (typing moves on once a segment is
/// full, which only the component knows). Over Blazor Server such a move lands a round trip later:
/// <list type="bullet">
///   <item>keys pressed meanwhile still come from an element the focus is leaving (<see cref="IsLeftBehind"/>): they are
///     meant for where it is going;</item>
///   <item>a focus event on an element asked for and overtaken since by the next move is an echo (<see cref="IsEcho"/>).
///     Any other focus event - on the element the keys came from, or anywhere else - is the user's (a click, Tab): the
///     component takes it and <see cref="Clear"/>s the moves.</item>
/// </list>
/// A move that has not landed within a second never will (the element went away): the elements stop counting then.
/// </summary>
/// <typeparam name="TKey">What identifies an element: a date, a segment.</typeparam>
internal sealed class BitBhFocusMoves<TKey> where TKey : notnull
{
    private const long Timeout = 1000;

    private readonly HashSet<TKey> _leftBehind = [];
    private readonly HashSet<TKey> _requested = [];
    private long _at;

    /// <summary>The focus is leaving <paramref name="from"/>: the keys still coming from it are meant for its target.</summary>
    public void Leaving(TKey from)
    {
        _leftBehind.Add(from);
        _at = Environment.TickCount64;
    }

    /// <summary>The browser is asked to focus <paramref name="to"/>.</summary>
    public void Requesting(TKey to)
    {
        _leftBehind.Remove(to);
        _requested.Add(to);
        _at = Environment.TickCount64;
    }

    /// <summary>Whether a key from <paramref name="key"/> was pressed before the focus left it.</summary>
    public bool IsLeftBehind(TKey key) => _leftBehind.Contains(key) && InFlight;

    /// <summary>
    /// Whether a focus event on <paramref name="key"/> (not the element the focus is on its way to) is that of a move
    /// overtaken since.
    /// </summary>
    public bool IsEcho(TKey key) => _requested.Contains(key) && InFlight;

    /// <summary>The focus has landed: where it was sent, or where the user put it.</summary>
    public void Clear()
    {
        _leftBehind.Clear();
        _requested.Clear();
    }

    private bool InFlight => Environment.TickCount64 - _at < Timeout;
}
