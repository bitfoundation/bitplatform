namespace Bit.Bmage;

/// <summary>The outcome of loading one image in a batch.</summary>
public sealed class BmageImageLoadResult : IDisposable
{
    private BmageImageLoadResult(BmageImageSource source, BmageImageBuffer? image, Exception? error)
    {
        Source = source;
        Image = image;
        Error = error;
    }

    public static BmageImageLoadResult Success(BmageImageSource source, BmageImageBuffer image) => new(source, image, null);
    public static BmageImageLoadResult Failure(BmageImageSource source, Exception error) => new(source, null, error);

    public BmageImageSource Source { get; }

    /// <summary>The decoded image, or null when loading failed. Owned by the caller.</summary>
    public BmageImageBuffer? Image { get; }

    public Exception? Error { get; }
    public bool IsSuccess => Image is not null;
    public string? FileName => Source.FileName;

    public void Dispose() => Image?.Dispose();
}
