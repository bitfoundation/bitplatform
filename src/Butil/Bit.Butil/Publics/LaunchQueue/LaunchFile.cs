namespace Bit.Butil;

/// <summary>
/// One file an installed app was launched with, as reported by
/// <see cref="LaunchQueue.SetConsumer(System.Action{LaunchParams})"/>.
/// </summary>
/// <remarks>
/// The file itself stays on the JS side - a <c>FileSystemFileHandle</c> can't cross interop. Read
/// and write it through <see cref="LaunchQueue.ReadText(LaunchFile)"/>,
/// <see cref="LaunchQueue.ReadBytes(LaunchFile)"/>, <see cref="LaunchQueue.WriteText(LaunchFile, string)"/>
/// and <see cref="LaunchQueue.WriteBytes(LaunchFile, byte[])"/>, or by this file's <see cref="Index"/>.
/// </remarks>
public class LaunchFile
{
    /// <summary>The launch this file came with - see <see cref="LaunchParams.LaunchId"/>.</summary>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>Position of this file in the launch, and the key its contents are read by.</summary>
    public int Index { get; set; }

    /// <summary>The file name, including its extension.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The MIME type the browser inferred, or empty when it couldn't.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Last-modified time, in milliseconds since the Unix epoch.</summary>
    public double LastModified { get; set; }
}
