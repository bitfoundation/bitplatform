namespace Bit.Butil;

public class NotificationOptions
{
    public string? Tag { get; set; }
    public string? Body { get; set; }
    public object? Data { get; set; }
    public string? Icon { get; set; }
    public string? Lang { get; set; }
    public bool? Silent { get; set; }
    public string? Badge { get; set; }
    public bool RequireInteraction { get; set; }
    public NotificationDirection Dir { get; set; }
}
