using System;
using System.IO;
using System.IO.Compression;

namespace Bit.BlazorUI.Build;

/// <summary>
/// The Brotli of the runtime MSBuild runs on, which WOFF2 compresses a font with: System.IO.Compression.BrotliStream where
/// that is .NET (the dotnet CLI), none where it is the .NET Framework (Visual Studio's msbuild.exe). This assembly targets
/// netstandard2.0, which has no Brotli of its own, so it is found by name.
/// </summary>
public static class Brotli
{
    private static readonly Type? StreamType = Type.GetType("System.IO.Compression.BrotliStream, System.IO.Compression.Brotli", throwOnError: false);

    // CompressionLevel.SmallestSize (.NET 6 and later), Brotli's quality 11 - what a font is compressed with once and
    // served many times. CompressionLevel.Optimal would be quality 4.
    private const CompressionLevel SmallestSize = (CompressionLevel)3;

    public static bool IsAvailable => StreamType is not null;

    public static byte[] Decompress(byte[] data, int offset, int count)
    {
        using var input = new MemoryStream(data, offset, count, writable: false);
        using var brotli = Create(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = (Stream)Activator.CreateInstance(StreamType ?? throw Unavailable(), output, SmallestSize, true)!)
        {
            brotli.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static Stream Create(Stream stream, CompressionMode mode) => (Stream)Activator.CreateInstance(StreamType ?? throw Unavailable(), stream, mode)!;

    private static NotSupportedException Unavailable() => new("this runtime has no Brotli (System.IO.Compression.BrotliStream)");
}
