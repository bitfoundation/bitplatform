namespace Bit.Bink;

/// <summary>Base class with no-op implementations so tools only override what they need.</summary>
public abstract class BinkToolBase : IBinkTool
{
    /// <inheritdoc />
    public abstract string Id { get; }

    /// <inheritdoc />
    public abstract string DisplayName { get; }

    /// <inheritdoc />
    public virtual string Cursor => "crosshair";

    /// <inheritdoc />
    public virtual bool WantsCoalescedInput => false;

    /// <inheritdoc />
    public virtual bool ModifiesDocument => true;

    /// <inheritdoc />
    public virtual bool WantsHover => false;

    /// <inheritdoc />
    public virtual void Activate(IBinkToolContext context) { }

    /// <inheritdoc />
    public virtual void Deactivate(IBinkToolContext context) { }

    /// <inheritdoc />
    public abstract bool OnPointerDown(IBinkToolContext context, in BinkInputSample sample);

    /// <inheritdoc />
    public virtual void OnPointerMove(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void OnPointerUp(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void OnPointerCancel(IBinkToolContext context, in BinkInputSample sample) { }

    /// <inheritdoc />
    public virtual void DrawOverlay(IBinkToolContext context, IBinkRenderContext render) { }

    /// <inheritdoc />
    public virtual bool OnKeyDown(IBinkToolContext context, BinkToolKeyEvent key) => false;
}
