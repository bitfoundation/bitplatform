using System.Diagnostics;

namespace Bit.BlazorUI;

/// <summary>
/// The press of a spin button: the step a press takes right away, and the continuous spin it turns into once the
/// button has been held for the press-and-hold delay. Every spin button of the inputs (the number field and the time
/// parts of the time picker, the date picker, the date range picker and the calendar) goes through it, so what tells a
/// click apart from a hold lives in one place.
/// </summary>
/// <remarks>
/// <para>
/// A press is <see cref="Press"/>ed when its pointerdown arrives, takes its step, and then asks for the
/// <see cref="Hold"/>; the pointerup (or pointerout, or pointercancel) <see cref="Release"/>s it. The release can arrive
/// while the step is still being taken - a step that awaits anything (an interop call, a consumer's event handler)
/// lets the events queued behind the pointerdown be handled in that gap - so the press remembers it, and a hold asked
/// for once the release has happened never starts. Without that a quick click would cancel a hold that is then
/// started afresh, leaving a spin nothing can stop.
/// </para>
/// <para>
/// The hold itself is a timer, and a timer races the input events of the browser: when the thread the timer runs on
/// is busy past the time the timer is due - a page that takes longer to render than the delay, which on WebAssembly
/// renders in the browser task of the event that caused it - the timer and a pointerup that arrived meanwhile are
/// both ready by the time the thread is free, and nothing says which of the two runs first (Firefox runs the timer).
/// So a timer that fired late is not trusted: the hold waits a little longer, which lets the input events that are
/// already queued be handled first, and only a timer that fired on time - proof that nothing was held up behind it -
/// starts the spin. Wherever the slow work comes from (the render of the step, a parent re-rendering, an interop round
/// trip) it shows up as a late timer, because the lateness is measured on the context the press is handled on.
/// </para>
/// </remarks>
internal sealed class BitSpinPress : IDisposable
{
    /// <summary>
    /// How late a timer may fire and still count as on time. Timers are never exact - browsers clamp and coalesce
    /// them by a few milliseconds - so this is what a timer that was merely imprecise stays within, and what one that
    /// was held up by a busy thread goes past.
    /// </summary>
    private const double LatenessTolerance = 25;

    /// <summary>
    /// How long the hold waits again after a timer that fired late: long enough for the input events queued while the
    /// thread was busy to be handled ahead of it, short enough to go unnoticed by a user really holding the button.
    /// </summary>
    private const int SettleDelay = 10;

    private bool _isPressed;
    private bool _isDisposed;
    private CancellationTokenSource? _holdCts;



    /// <summary>
    /// Whether a hold has been started and not yet released.
    /// </summary>
    public bool IsHolding => _holdCts is not null;



    /// <summary>
    /// Records the press of a spin button. It is called before the press takes its step, and before anything is
    /// awaited, so a release that arrives while the step is being taken is not lost.
    /// </summary>
    public void Press()
    {
        if (_isDisposed) return;

        CancelHold();

        _isPressed = true;
    }

    /// <summary>
    /// Ends the press, stopping its continuous spin or the delay before it.
    /// </summary>
    public void Release()
    {
        _isPressed = false;

        CancelHold();
    }

    /// <summary>
    /// Starts the continuous spin once the button has been held for the given delay, unless it is released before
    /// that (or was released already, while the step of the press was being taken). The spin is handed a token that is
    /// cancelled when the press ends, and runs on the context this is called from.
    /// </summary>
    /// <remarks>
    /// The hold is deliberately not awaited: it lives as long as the button is held, so awaiting it would leave the
    /// pointerdown event handler (and the render it drives) pending for the whole duration of the press.
    /// </remarks>
    public void Hold(int delay, Func<CancellationToken, Task> spin)
    {
        if (_isPressed is false || _isDisposed) return;

        CancelHold();

        _holdCts = new();

        _ = HoldAsync(delay, spin, _holdCts.Token);
    }

    public void Dispose()
    {
        _isDisposed = true;

        Release();
    }



    private static async Task HoldAsync(int delay, Func<CancellationToken, Task> spin, CancellationToken token)
    {
        try
        {
            await WaitForHoldAsync(delay, token);

            // The release can be handled between the timer firing and this continuation running.
            token.ThrowIfCancellationRequested();

            await spin(token);
        }
        catch (OperationCanceledException) { } // the button was released before the continuous spin started
        catch (ObjectDisposedException) { } // the component went away while the spin was starting
    }

    /// <summary>
    /// Waits out the delay of a hold, and then for as long as the timer it waits on fires late (see the remarks of
    /// the class).
    /// </summary>
    private static async Task WaitForHoldAsync(int delay, CancellationToken token)
    {
        var wait = Math.Max(1, delay);

        while (true)
        {
            var start = Stopwatch.GetTimestamp();

            // Not configured to leave the context: the continuation has to be queued where the input events are
            // handled, so the time it took to get to run is what tells whether anything was held up behind it.
            await Task.Delay(wait, token);

            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds - wait <= LatenessTolerance) return;

            wait = SettleDelay;
        }
    }

    private void CancelHold()
    {
        if (_holdCts is null) return;

        // The token handed to the hold stays usable after this: a cancelled token reads as cancelled even once its
        // source is disposed.
        _holdCts.Cancel();
        _holdCts.Dispose();
        _holdCts = null;
    }
}
