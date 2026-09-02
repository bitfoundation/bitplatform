namespace Bit.Butil;

/// <summary>One scanned NDEF message.</summary>
public class NdefMessage
{
    /// <summary>The tag's serial number, when the platform exposes one.</summary>
    public string SerialNumber { get; set; } = string.Empty;
    
    /// <summary>The records the message carries.</summary>
    public NdefRecord[] Records { get; set; } = [];
}
