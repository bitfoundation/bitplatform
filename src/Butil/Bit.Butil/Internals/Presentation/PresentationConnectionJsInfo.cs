namespace Bit.Butil;

/// <summary>
/// What JS reports about a presentation connection it has just opened: the presentation's own id,
/// the URL that was accepted, and the state it started in.
/// </summary>
internal class PresentationConnectionJsInfo
{
    public string ConnectionId { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;
}
