namespace Bit.BlazorUI;

/// <summary>
/// Identifies the author of a message.
/// </summary>
/// <remarks>
/// Modelled as a value type wrapping a string rather than an <see langword="enum"/> so that
/// applications and providers can introduce roles the framework does not know about
/// (for example <c>"critic"</c> in a multi-agent debate) without forking the library.
/// Comparison is ordinal and case-insensitive.
/// </remarks>
[System.Text.Json.Serialization.JsonConverter(typeof(BitAiRoleJsonConverter))]
public readonly record struct BitAiRole : IComparable<BitAiRole>
{
    /// <summary>A message authored by the end user.</summary>
    public static readonly BitAiRole User = new("user");

    /// <summary>A message authored by the model or agent.</summary>
    public static readonly BitAiRole Assistant = new("assistant");

    /// <summary>An instruction message that conditions the model.</summary>
    public static readonly BitAiRole System = new("system");

    /// <summary>A message carrying the output of a tool back into the conversation.</summary>
    public static readonly BitAiRole Tool = new("tool");

    /// <summary>A developer-authored instruction message, distinct from an end-user system prompt.</summary>
    public static readonly BitAiRole Developer = new("developer");

    /// <summary>
    /// Creates a role from its wire value.
    /// </summary>
    /// <param name="value">The role identifier. Must not be <see langword="null"/> or whitespace.</param>
    public BitAiRole(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>The underlying role identifier, as it appears on the wire.</summary>
    public string Value { get; }

    /// <summary><see langword="true"/> when this role is <see cref="User"/>.</summary>
    public bool IsUser => Equals(User);

    /// <summary><see langword="true"/> when this role is <see cref="Assistant"/>.</summary>
    public bool IsAssistant => Equals(Assistant);

    /// <summary><see langword="true"/> when this role is <see cref="System"/> or <see cref="Developer"/>.</summary>
    public bool IsSystem => Equals(System) || Equals(Developer);

    /// <summary><see langword="true"/> when this role is <see cref="Tool"/>.</summary>
    public bool IsTool => Equals(Tool);

    /// <inheritdoc />
    public bool Equals(BitAiRole other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode() => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    /// <inheritdoc />
    public int CompareTo(BitAiRole other) => string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;

    /// <summary>Converts a string to an <see cref="BitAiRole"/>.</summary>
    /// <param name="value">The role identifier.</param>
    public static implicit operator BitAiRole(string value) => new(value);
}
