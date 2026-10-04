namespace Bit.BlazorUI;

/// <summary>
/// A component that takes its defaults from a params object through a <see cref="BitCascadeTracker"/>: what the
/// tracker has to ask of it to tell the component's own values from the ones the params object supplied.
/// </summary>
/// <remarks>
/// Every <see cref="BitComponentBase"/> is one. A component that cannot derive from it - one built on a framework
/// base class, as <c>BitErrorBoundary</c> is on <c>ErrorBoundaryBase</c> - implements it to share the same engine.
/// </remarks>
internal interface IBitCascadeTarget
{
    /// <summary>
    /// Whether the markup set the named parameter on this render.
    /// </summary>
    bool IsSetByMarkup(string name);

    /// <summary>
    /// What the component holds of its own for its <c>Dir</c> parameter, which reads through to the direction
    /// cascaded from above while it is not set - so this backing value, and not the direction the component happens
    /// to show, is what has to be recorded and put back.
    /// </summary>
    BitDir? OwnDir { get; set; }
}
