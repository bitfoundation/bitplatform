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
    /// Whether the Modal has reported itself out of the way.
    /// </summary>
    internal bool HasLeft { get; private set; }

    /// <summary>
    /// Reports the Modal out of the way: closed, and done animating.
    /// </summary>
    internal void Left()
    {
        if (HasLeft) return;

        HasLeft = true;

        _onLeft();
    }
}
