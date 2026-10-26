namespace Bit.Bdnd;

/// <summary>Raw payload shape received from JS for a native drop.</summary>
public sealed class BdndNativeDropPayload
{
    /// <summary>The DataTransfer types of the drop.</summary>
    public string[] Types { get; set; } = [];

    /// <summary>The string data of the drop, keyed by DataTransfer type.</summary>
    public Dictionary<string, string> Data { get; set; } = [];

    /// <summary>The metadata of the dropped files.</summary>
    public BdndNativeFile[] Files { get; set; } = [];

    /// <summary>The id under which the browser module keeps the drop's files for reading; null when it keeps none.</summary>
    public string? DropId { get; set; }

    /// <summary>How many files the drop carried, before the target's file types and file limit were applied.</summary>
    public int TotalFiles { get; set; }

    /// <summary>
    /// How many files the drop hands over: <see cref="Files"/> holds the first page of them, the rest are read from the
    /// module page by page (a Blazor Server message has a size limit).
    /// </summary>
    public int FileCount { get; set; }

    /// <summary>Whether the dropped folders could not be listed in full.</summary>
    public bool Incomplete { get; set; }
}
