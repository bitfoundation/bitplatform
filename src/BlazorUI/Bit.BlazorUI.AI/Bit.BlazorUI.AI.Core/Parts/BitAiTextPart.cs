using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// A run of text that grows as the model writes it.
/// </summary>
/// <remarks>
/// <para>
/// Text is accumulated in a <see cref="StringBuilder"/> and materialised lazily, so appending a
/// token is amortised constant time rather than the quadratic string concatenation that naive
/// streaming implementations fall into on long responses.
/// </para>
/// <para>
/// The assistant body is a text part by default and is rendered as Markdown unless the host turns
/// that off. Use <see cref="BitAiMarkdownPart"/> only when the distinction has to survive serialization.
/// </para>
/// <para>
/// Safe for one writer and any number of readers. Transports usually apply deltas off the
/// renderer's synchronization context while components read <see cref="Text"/> during render, and
/// a <see cref="StringBuilder"/> read mid-append throws, so the builder is only touched under a lock
/// and readers are handed an immutable snapshot that is cached until the next append.
/// </para>
/// </remarks>
public class BitAiTextPart : BitAiMessagePart, IBitAiTextualPart, IBitAiStreamablePart
{
    private readonly StringBuilder _builder;
    private readonly object _gate = new();
    private volatile string? _cachedText;
    private volatile int _length;
    private volatile bool _isStreaming;

    /// <summary>Creates an empty part that expects more text.</summary>
    public BitAiTextPart() : this(null, null) { }

    /// <summary>Creates a finished part with known content.</summary>
    /// <param name="text">The text.</param>
    public BitAiTextPart(string text) : this(text ?? throw new ArgumentNullException(nameof(text)), null) { }

    /// <summary>
    /// Creates a part with an explicit identifier, for transports that address parts directly.
    /// </summary>
    /// <param name="text">
    /// The text, or <see langword="null"/> for an empty part that expects more.
    /// </param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    /// <remarks>
    /// Both parameters are required rather than optional. An optional overload would make
    /// <c>new BitAiTextPart("hello")</c> ambiguous between text and identifier, and the compiler resolves
    /// that ambiguity the wrong way round.
    /// </remarks>
    public BitAiTextPart(string? text, string? partId) : base(partId)
    {
        _builder = new StringBuilder(text ?? string.Empty, 256);
        _cachedText = text;
        _length = _builder.Length;
        _isStreaming = text is null;
    }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Text;

    /// <summary>The text accumulated so far.</summary>
    public string Text
    {
        get
        {
            if (_cachedText is { } cached) return cached;
            lock (_gate) return _cachedText ??= _builder.ToString();
        }
    }

    /// <summary>How many characters have arrived, without materialising the string.</summary>
    /// <remarks>
    /// Prefer this over <c>Text.Length</c> in hot paths such as auto-scroll checks: it avoids
    /// building the string.
    /// </remarks>
    public int Length => _length;

    /// <inheritdoc />
    public bool IsStreaming => _isStreaming;

    /// <summary><see langword="true"/> when no text has arrived yet.</summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// Appends text and notifies observers.
    /// </summary>
    /// <param name="delta">The text to append. An empty delta is ignored.</param>
    public void Append(string? delta)
    {
        if (string.IsNullOrEmpty(delta)) return;
        lock (_gate)
        {
            _builder.Append(delta);
            _length = _builder.Length;
            _cachedText = null;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Replaces the whole content, for edits and regenerations.
    /// </summary>
    /// <param name="text">The new text.</param>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        lock (_gate)
        {
            _builder.Clear();
            _builder.Append(text);
            _length = _builder.Length;
            _cachedText = text;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Marks the part finished, which lets renderers do work they deferred while it was growing.
    /// </summary>
    public void Complete()
    {
        lock (_gate)
        {
            if (!_isStreaming) return;
            _isStreaming = false;
        }

        OnCompleted();
        NotifyChanged();
    }

    /// <summary>Called once when the part finishes, before observers are notified.</summary>
    protected virtual void OnCompleted() { }

    /// <inheritdoc />
    public override string ToString() => Text;
}

/// <summary>
/// Text that is explicitly Markdown, for hosts that render plain text parts verbatim.
/// </summary>
public sealed class BitAiMarkdownPart : BitAiTextPart
{
    /// <summary>Creates an empty part that expects more text.</summary>
    public BitAiMarkdownPart() : base(null, null) { }

    /// <summary>Creates a finished part with known content.</summary>
    /// <param name="text">The Markdown source.</param>
    public BitAiMarkdownPart(string text) : base(text ?? throw new ArgumentNullException(nameof(text)), null) { }

    /// <summary>Creates a part with an explicit identifier.</summary>
    /// <param name="text">The Markdown source, or <see langword="null"/> for an empty part.</param>
    /// <param name="partId">The identifier, or <see langword="null"/> to generate one.</param>
    public BitAiMarkdownPart(string? text, string? partId) : base(text, partId) { }

    /// <inheritdoc />
    public override string Kind => BitAiMessagePartKinds.Markdown;
}
