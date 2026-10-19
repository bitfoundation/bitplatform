using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Bmage.Interop;

/// <summary>
/// The single JavaScript boundary in Bmage. Everything the browser must do (format codecs, capability probing,
/// clipboard, camera, downloads, text measurement) goes through here; all image processing stays in C#.
/// </summary>
/// <remarks>
/// Pixel and file bytes cross the boundary as raw streams rather than base64 JSON, so a 24 megapixel image costs one
/// buffer copy instead of a 33% inflated string. Metadata travels separately as a tiny JSON payload.
/// </remarks>
public sealed class BrowserImageInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/Bit.Bmage/bit-bmage.js";

    /// <summary>Members the JSON serialiser needs on interop result types, so trimming keeps them.</summary>
    private const DynamicallyAccessedMemberTypes JsonMembers =
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties;

    private readonly IJSRuntime _jsRuntime;
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;
    private bool _disposed;

    public BrowserImageInterop(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));

    /// <summary>
    /// Maximum number of bytes accepted from a single JavaScript stream. Guards against a hostile or buggy page
    /// streaming unbounded data into the .NET heap. Defaults to 512 MB.
    /// </summary>
    public long MaxStreamBytes { get; set; } = 512L * 1024 * 1024;

    /// <summary>True once the JS module has been loaded.</summary>
    public bool IsLoaded => _module is not null;

    /// <summary>
    /// Loads the JS module if needed. Callers should invoke this from <c>OnAfterRenderAsync</c> rather than during
    /// prerendering, where no browser exists.
    /// </summary>
    public async ValueTask<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_module is { } loaded) return loaded;
        await _moduleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_module is { } existing) return existing;
            try
            {
                var module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);
                if (_disposed)
                {
                    // Disposal ran while the import was in flight. Release what we just imported rather than
                    // storing it, or the reference would outlive the object that owns it.
                    await module.DisposeAsync().ConfigureAwait(false);
                    throw new ObjectDisposedException(nameof(BrowserImageInterop));
                }
                _module = module;
                return module;
            }
            catch (JSException ex)
            {
                throw new ImageCapabilityException(
                    $"Could not load the Bmage browser module from '{ModulePath}'. " +
                    "Check that the Bit.Bmage static web assets are served, and that this code runs after the component is interactive rather than during prerendering. " +
                    $"The browser reported: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                throw new ImageCapabilityException(
                    "JavaScript interop is not available yet. Browser-backed image operations only work once the component is interactive; " +
                    "call them from OnAfterRenderAsync or an event handler, not during prerendering or OnInitializedAsync on the server. " +
                    $"The runtime reported: {ex.Message}", ex);
            }
        }
        finally
        {
            _moduleLock.Release();
        }
    }

    /// <summary>Invokes a module function that returns no value.</summary>
    public async ValueTask InvokeVoidAsync(string identifier, CancellationToken cancellationToken = default, params object?[] args)
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync(identifier, cancellationToken, args).ConfigureAwait(false);
    }

    /// <summary>Invokes a module function that returns a JSON-serialisable value.</summary>
    public async ValueTask<TResult> InvokeAsync<[DynamicallyAccessedMembers(JsonMembers)] TResult>(string identifier, CancellationToken cancellationToken = default, params object?[] args)
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        return await module.InvokeAsync<TResult>(identifier, cancellationToken, args).ConfigureAwait(false);
    }

    /// <summary>
    /// Invokes a module function that returns a byte result handle, reads its metadata and payload, then releases the
    /// browser-side buffer.
    /// </summary>
    public async ValueTask<BrowserBytes<TInfo>> InvokeForBytesAsync<[DynamicallyAccessedMembers(JsonMembers)] TInfo>(string identifier, CancellationToken cancellationToken = default, params object?[] args)
        where TInfo : class
    {
        var module = await GetModuleAsync(cancellationToken).ConfigureAwait(false);
        IJSObjectReference? handle = null;
        try
        {
            handle = await module.InvokeAsync<IJSObjectReference?>(identifier, cancellationToken, args).ConfigureAwait(false);
            if (handle is null) return BrowserBytes<TInfo>.Empty;
            var info = await handle.InvokeAsync<TInfo>("info", cancellationToken).ConfigureAwait(false);
            var bytes = await ReadBytesAsync(handle, cancellationToken).ConfigureAwait(false);
            return new BrowserBytes<TInfo>(info, bytes);
        }
        finally
        {
            if (handle is not null)
            {
                try
                {
                    await handle.InvokeVoidAsync("dispose", CancellationToken.None).ConfigureAwait(false);
                }
                catch (JSException) { /* the page may already be gone */ }
                catch (JSDisconnectedException) { /* circuit closed */ }
                catch (ObjectDisposedException) { /* runtime shutting down */ }
                await handle.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Reads a byte handle's payload directly into a pooled buffer, avoiding the intermediate array a plain
    /// <c>byte[]</c> return would allocate.
    /// </summary>
    private async ValueTask<PooledBytes> ReadBytesAsync(IJSObjectReference handle, CancellationToken cancellationToken)
    {
        await using var reference = await handle.InvokeAsync<IJSStreamReference>("bytes", cancellationToken).ConfigureAwait(false);
        if (reference.Length > MaxStreamBytes)
            throw new ImageLimitExceededException($"The browser returned {reference.Length} bytes, which exceeds the {MaxStreamBytes} byte interop limit. Raise BrowserImageInterop.MaxStreamBytes if this is expected.");
        // A single .NET buffer cannot exceed int.MaxValue, so refuse here rather than truncating the cast and
        // renting a nonsensical length. This only triggers when MaxStreamBytes has been raised above 2 GB.
        if (reference.Length > int.MaxValue)
            throw new ImageLimitExceededException($"The browser returned {reference.Length} bytes, which is larger than a single buffer can hold.");
        var length = (int)reference.Length;
        await using var stream = await reference.OpenReadStreamAsync(MaxStreamBytes, cancellationToken).ConfigureAwait(false);
        var owner = MemoryPool<byte>.Shared.Rent(length);
        try
        {
            var memory = owner.Memory[..length];
            var read = 0;
            while (read < length)
            {
                var n = await stream.ReadAsync(memory[read..], cancellationToken).ConfigureAwait(false);
                if (n == 0) break;
                read += n;
            }
            if (read < length)
            {
                owner.Dispose();
                throw new ImageDecodeException($"The browser stream ended after {read} of {length} bytes.");
            }
            return new PooledBytes(owner, length);
        }
        catch
        {
            owner.Dispose();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        var module = _module;
        _module = null;
        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("releaseResources").ConfigureAwait(false);
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException) { /* circuit already closed */ }
            catch (JSException) { /* page navigating away */ }
            catch (ObjectDisposedException) { /* runtime shutting down */ }
        }
        _moduleLock.Dispose();
    }
}

/// <summary>Bytes returned from the browser together with their descriptor. Dispose to return the buffer to the pool.</summary>
public readonly struct BrowserBytes<TInfo> : IDisposable where TInfo : class
{
    public BrowserBytes(TInfo? info, PooledBytes bytes)
    {
        Info = info;
        Bytes = bytes;
    }

    /// <summary>An empty result, used when the browser had nothing to return (e.g. an empty clipboard).</summary>
    public static BrowserBytes<TInfo> Empty => new(null, default);

    /// <summary>The metadata descriptor, or null when the result was empty.</summary>
    public TInfo? Info { get; }

    /// <summary>The payload.</summary>
    public PooledBytes Bytes { get; }

    /// <summary>True when the browser returned a result.</summary>
    public bool HasValue => Info is not null;

    public void Dispose() => Bytes.Dispose();
}

/// <summary>A rented byte buffer exposing exactly the bytes that were read.</summary>
public readonly struct PooledBytes : IDisposable
{
    private readonly IMemoryOwner<byte>? _owner;

    public PooledBytes(IMemoryOwner<byte> owner, int length)
    {
        _owner = owner;
        Length = length;
    }

    /// <summary>Number of valid bytes.</summary>
    public int Length { get; }

    /// <summary>The valid bytes.</summary>
    public ReadOnlyMemory<byte> Memory => _owner is null ? ReadOnlyMemory<byte>.Empty : _owner.Memory[..Length];

    /// <summary>The valid bytes as a span.</summary>
    public ReadOnlySpan<byte> Span => Memory.Span;

    /// <summary>Copies the bytes into a new array. Prefer <see cref="Span"/> where a copy is not needed.</summary>
    public byte[] ToArray() => Span.ToArray();

    public void Dispose() => _owner?.Dispose();
}
