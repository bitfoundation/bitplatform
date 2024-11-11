namespace Bit.Butil;

internal class InternalNotificationOptions(NotificationOptions options)
{
    public string? badge { get; set; } = options.Badge;
    public string? body { get; set; } = options.Body;
    public object? data { get; set; } = options.Data;
    public string? dir { get; set; } = options.Dir.ToString().ToLower();
    public string? icon { get; set; } = options.Icon;
    public string? image { get; set; } = options.Image;
    public string? lang { get; set; } = options.Lang;
    public bool? renotify { get; set; } = options.Renotify;
    public bool requireInteraction { get; set; } = options.RequireInteraction;
    public bool? silent { get; set; } = options.Silent;
    public string? tag { get; set; } = options.Tag;
    public long? timestamp { get; set; } = options.Timestamp;
    public int[]? vibrate { get; set; } = options.Vibrate;
}
