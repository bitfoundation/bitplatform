namespace Bit.Butil;

/// <summary>How much of an idle period is left, handed to <see cref="Scheduler.RequestIdleCallback"/>'s callback.</summary>
/// <param name="DidTimeout">
/// True when the callback ran because its timeout expired rather than because the browser went
/// idle. In that case <paramref name="TimeRemaining"/> is zero and the page is busy - do the
/// smallest useful amount of work, or none.
/// </param>
/// <param name="TimeRemaining">
/// Milliseconds of idle time left, as measured at the moment the callback was dispatched. It is a
/// snapshot, not a budget that updates: the real value falls as you use it, and crossing into
/// negative territory is what makes an idle callback janky. Treat 50ms as the ceiling the browser
/// will ever report.
/// </param>
public record IdleDeadline(bool DidTimeout, double TimeRemaining);
