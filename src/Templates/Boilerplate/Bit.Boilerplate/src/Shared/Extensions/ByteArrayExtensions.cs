namespace System;

public static class ByteArrayExtensions
{
    public static string ToStampString(this byte[]? source)
    {
        if (source is null or { Length: 0 })
        {
            source = [0, 0, 0, 0, 0, 0, 0, 0];
        }

        var base64String = Convert.ToBase64String(source);

        return Uri.EscapeDataString(base64String);
    }
}
