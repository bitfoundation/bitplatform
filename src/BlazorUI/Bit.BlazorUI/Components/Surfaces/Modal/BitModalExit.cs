namespace Bit.BlazorUI;

/// <summary>
/// What a modal container hands the <see cref="BitModal"/> it renders for one modal of its service, so the two can
/// agree on when the modal leaves the page: the service closes a modal at once - its result is in, it is no longer
/// open - but the container keeps rendering it, closed, until its exit animation has played.
/// </summary>
/// <remarks>
/// Cascaded under a name of its own rather than put on the parameters, which are the consumer's objects and can be
/// shared between modals. A Modal stops the cascade at its content, so a Modal declared inside a modal the service
/// shows is never mistaken for that modal.
/// <br/>
/// Only a container that cascades one keeps a closed modal in the page: one that does not (a container a consumer
/// wrote by hand) has nothing to hear the modal out through, and takes it out of the page at once, as it always has.
/// </remarks>
internal sealed class BitModalExit
{
    internal const string CascadingName = "BitModal.Exit";

    private readonly Action _onLeft;



    internal BitModalExit(Action onLeft)
    {
        _onLeft = onLeft;
    }



    /// <summary>
    /// Whether the Modal has started closing: from here on it is the Modal that says when it is out of the way, so
    /// the container no longer needs a deadline of its own to take it out of the page by.
    /// </summary>
    internal bool IsClosing { get; private set; }

    /// <summary>
    /// Whether the Modal has reported itself out of the way.
    /// </summary>
    internal bool HasLeft { get; private set; }

    /// <summary>
    /// Reports the Modal closing: the close sequence has started, and ends in <see cref="Left"/> however long the
    /// browser takes over it - on a slow circuit the round trips it makes before the animation even starts can
    /// take longer than the animation itself.
    /// </summary>
    internal void Closing()
    {
        IsClosing = true;
    }

    /// <summary>
    /// Reports the Modal out of the way: closed, and done animating.
    /// </summary>
    internal void Left()
    {
        if (HasLeft) return;

        HasLeft = true;
        IsClosing = true;

        _onLeft();
    }
}
