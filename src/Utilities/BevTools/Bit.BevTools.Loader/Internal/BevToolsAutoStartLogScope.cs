namespace Bit.BevTools;

/// <summary>
/// One scope (<c>ILogger.BeginScope</c>) of a log entry captured before BevTools was attached: its key/value pairs when
/// it has them (ASP.NET Core's request scope, a dictionary), each kept as <see cref="BevToolsAutoStartLogValue"/>, or
/// otherwise its text and type, as the attached session's logger provider would read it.
/// </summary>
internal sealed class BevToolsAutoStartLogScope
{
    /// <summary>The scope's pairs, when it is a key/value scope.</summary>
    public IReadOnlyList<BevToolsAutoStartLogValue>? Pairs { get; init; }

    /// <summary>The scope's text, or its <c>ToString</c>, when it is not a key/value scope; clipped.</summary>
    public string? Text { get; init; }

    public Type? Type { get; init; }

    public bool IsString => Type == typeof(string);
}
