namespace Bit.Belemetry;

/// <summary>The consent and user context the browser script reports (<c>getContext()</c> / <c>Belemetry.ContextChanged</c>).</summary>
internal sealed class BelemetryJsContext
{
    public int Consent { get; set; }

    public bool Decided { get; set; }

    public string? SessionId { get; set; }

    public string? PseudoId { get; set; }

    public string? UserId { get; set; }
}
