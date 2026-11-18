namespace Bit.Blow;

/// <summary>
/// One problem a validation found, and where: the node, edge and port it concerns, so a UI can mark the element
/// and take the user to it.
/// </summary>
/// <param name="Code">A stable, machine-readable code; the built-in ones are listed in <see cref="BlowValidationCodes"/>.</param>
/// <param name="Severity">How serious the problem is.</param>
/// <param name="Message">An English description naming the elements involved. Localize by <see cref="Code"/>.</param>
public sealed record BlowValidationIssue(string Code, BlowValidationSeverity Severity, string Message)
{
    /// <summary>The node the issue concerns, if any.</summary>
    public string? NodeId { get; init; }

    /// <summary>The edge the issue concerns, if any.</summary>
    public string? EdgeId { get; init; }

    /// <summary>The id of the port the issue concerns (null for a node's unnamed port), when <see cref="PortType"/> is set.</summary>
    public string? PortId { get; init; }

    /// <summary>The type of the port the issue concerns, or null when it concerns no port.</summary>
    public BlowHandleType? PortType { get; init; }
}
