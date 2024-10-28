namespace System;

public static class ByteArrayExtensions
{
    public static string AsStringConcurrencyStamp(this byte[]? source)
    {
        if (source is null or { Length: 0 })
            source = [0, 0, 0, 0, 0, 0, 0, 0];

        return Convert.ToBase64String(source);
    }
}
