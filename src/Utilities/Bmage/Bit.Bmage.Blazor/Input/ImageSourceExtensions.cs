using Microsoft.AspNetCore.Components.Forms;

namespace Bit.Bmage.Input;

/// <summary>
/// The <see cref="ImageSource"/> factory for Blazor's <see cref="IBrowserFile"/>, added to <see cref="ImageSource"/>
/// itself: <c>ImageSource.FromBrowserFile(file)</c> reads like the factories Bit.Bmage ships.
/// </summary>
/// <remarks>
/// This container is never named in code - the member is an extension member on <see cref="ImageSource"/>, visible
/// wherever <c>Bit.Bmage.Input</c> is in scope. A consumer compiling at a language version older than C# 14 cannot read
/// an extension member and calls <c>ImageSourceExtensions.FromBrowserFile(file)</c> instead.
/// </remarks>
public static class ImageSourceExtensions
{
    extension(ImageSource)
    {
        /// <summary>Wraps an <see cref="IBrowserFile"/> from an <c>InputFile</c> component or a drop zone.</summary>
        public static ImageSource FromBrowserFile(IBrowserFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            return new BrowserFileSource(file);
        }
    }

    private sealed class BrowserFileSource : ImageSource
    {
        private readonly IBrowserFile _file;

        public BrowserFileSource(IBrowserFile file)
        {
            _file = file;
            FileName = file.Name;
            ContentType = file.ContentType;
            Length = file.Size;
        }

        public override async ValueTask<PooledBuffer> ReadAsync(long maxBytes, CancellationToken cancellationToken = default)
        {
            if (_file.Size > maxBytes)
                throw new ImageLimitExceededException($"'{_file.Name}' is {_file.Size} bytes, which exceeds the {maxBytes} byte limit for a single image.");
            await using var stream = _file.OpenReadStream(maxBytes, cancellationToken);
            return await PooledBuffer.ReadAllAsync(stream, (int)_file.Size, maxBytes, cancellationToken).ConfigureAwait(false);
        }
    }
}
