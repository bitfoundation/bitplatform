namespace Bit.BlazorUI;

/// <summary>
/// The base class for message parts, providing identity and fine-grained change notification.
/// </summary>
/// <remarks>
/// <para>
/// Change notification is per part, not per message. During streaming only the part that grew
/// raises <see cref="Changed"/>, so a message holding a long reasoning block, three code blocks, and
/// two tool calls re-renders one small component per token instead of the whole message.
/// </para>
/// <para>
/// <see cref="Version"/> exists so components can implement a cheap <c>ShouldRender</c> without
/// comparing content: store the version seen at the last render and compare integers.
/// </para>
/// </remarks>
public abstract class BitAiMessagePart : IBitAiMessagePart
{
    private int _version;

    /// <summary>Creates a part.</summary>
    /// <param name="partId">
    /// The identifier, or <see langword="null"/> to generate one. Supply it when a transport
    /// addresses parts explicitly so later deltas can find this instance.
    /// </param>
    protected BitAiMessagePart(string? partId = null) => PartId = partId ?? BitAiId.NewPartId();

    /// <inheritdoc />
    public string PartId { get; }

    /// <inheritdoc />
    public abstract string Kind { get; }

    /// <summary>
    /// Increments on every change. Compare it against the version seen at the last render to decide
    /// whether a re-render is needed.
    /// </summary>
    public int Version => _version;

    /// <summary>The message this part belongs to, once it has been added to one.</summary>
    public BitAiMessage? Message { get; internal set; }

    /// <summary>Raised after the part changes.</summary>
    /// <remarks>
    /// Raised synchronously on whichever thread applied the change. Blazor components must marshal
    /// onto the renderer through <c>InvokeAsync</c>, which the built-in components do for you.
    /// </remarks>
    public event Action<BitAiMessagePart>? Changed;

    /// <summary>
    /// Bumps <see cref="Version"/>, raises <see cref="Changed"/>, and tells the owning message.
    /// </summary>
    /// <remarks>Call this at the end of every mutating operation on a derived part.</remarks>
    protected internal void NotifyChanged()
    {
        Interlocked.Increment(ref _version);
        Changed?.Invoke(this);
        Message?.OnPartChanged(this);
    }
}
