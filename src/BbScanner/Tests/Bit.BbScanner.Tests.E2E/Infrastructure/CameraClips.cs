namespace Bit.BbScanner.Tests.E2E.Infrastructure;

/// <summary>
/// The Y4M clips the fake camera plays, written once per run into a directory of their own and deleted when
/// the run ends. See <see cref="FakeCameraVideo"/> for what is in them.
/// </summary>
public sealed class CameraClips : IDisposable
{
    private CameraClips(string directory) => Directory = directory;

    /// <summary>Where the clips are, which is also where a test may write an image of its own.</summary>
    public string Directory { get; }

    /// <summary>The rotating multi symbology clip.</summary>
    public string Rotating => Path.Combine(Directory, "rotating.y4m");

    /// <summary>A clip that contains no barcode.</summary>
    public string Blank => Path.Combine(Directory, "blank.y4m");

    /// <summary>A clip showing a single QR code for its whole length.</summary>
    public string SingleQr => Path.Combine(Directory, "single.y4m");

    public static CameraClips Write()
    {
        var clips = new CameraClips(Path.Combine(Path.GetTempPath(), "bit-bbscanner-e2e", Guid.NewGuid().ToString("N")));

        System.IO.Directory.CreateDirectory(clips.Directory);
        FakeCameraVideo.WriteRotatingSymbols(clips.Rotating);
        FakeCameraVideo.WriteBlank(clips.Blank);
        FakeCameraVideo.WriteSingleQr(clips.SingleQr, "SINGLE-QR");

        return clips;
    }

    public void Dispose()
    {
        try
        {
            if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
            // A file still held by a browser process; the temp directory is cleaned up by the operating system.
        }
        catch (UnauthorizedAccessException)
        {
            // Same, on Windows.
        }
    }
}
