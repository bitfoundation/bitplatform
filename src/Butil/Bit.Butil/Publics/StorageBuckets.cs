using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Storage_Buckets_API">Storage Buckets API</see>
/// (<c>navigator.storageBuckets</c>): named compartments of the origin's storage, each with its own
/// quota, persistence, durability and expiry.
/// </summary>
/// <remarks>
/// Without buckets an origin has one pile of storage that the browser evicts whole: the draft the
/// user has not sent yet goes out with the thumbnail cache. A bucket is the unit that fixes that -
/// put the re-fetchable things in one bucket and the irreplaceable ones in another, and only the
/// first is worth evicting.
/// <br/>
/// Each bucket owns its own IndexedDB databases, Cache Storage caches and file system. The file
/// members here are that file system - the same
/// <see cref="OriginPrivateFileSystem">origin private file system</see>, rooted in the bucket rather
/// than in the origin.
/// <br/>
/// Chromium-only at the time of writing. Every member degrades to a false/null/empty answer where
/// the API is absent, so a call is safe to make before checking <see cref="IsSupported"/> - it just
/// won't do anything.
/// </remarks>
[ButilService(typeof(StorageBuckets))]
public class StorageBuckets(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>navigator.storageBuckets</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.storageBuckets.isSupported");

    /// <summary>
    /// Opens the named bucket, creating it when it isn't there.
    /// </summary>
    /// <param name="name">
    /// The bucket name: lower-case letters, digits, <c>-</c> and <c>_</c>, not starting with <c>-</c>
    /// or <c>_</c>. A name outside that alphabet is rejected by the browser and comes back as null.
    /// </param>
    /// <param name="persisted">Ask for a bucket exempt from eviction. The browser decides - read <see cref="StorageBucketInfo.Persisted"/> for the answer.</param>
    /// <param name="durability">How hard the browser should try not to lose the latest writes.</param>
    /// <param name="quota">A ceiling in bytes for this bucket, which can only be lower than the origin's own quota.</param>
    /// <param name="expires">When the browser may delete the bucket. Null leaves it without an expiry.</param>
    /// <returns>The bucket's state, or null when the API is absent or the name was rejected.</returns>
    /// <remarks>
    /// The options only apply when the bucket is created: opening an existing bucket returns it as
    /// it is, so this is safe to call on every start-up.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageBucketInfo))]
    public ValueTask<StorageBucketInfo?> Open(string name,
                                              bool persisted = false,
                                              StorageBucketDurability durability = StorageBucketDurability.Default,
                                              long? quota = null,
                                              DateTimeOffset? expires = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return js.Invoke<StorageBucketInfo?>("BitButil.storageBuckets.open",
            name, persisted, Durability(durability), quota, expires?.ToUnixTimeMilliseconds());
    }

    /// <summary>Reads a bucket's current state without changing any of it.</summary>
    /// <param name="name">The bucket name.</param>
    /// <returns>The bucket's state, or null when the API is absent.</returns>
    /// <remarks>
    /// Opening a bucket is what creates it, so asking about a name that was never used creates an
    /// empty bucket under that name. Check <see cref="Keys"/> first when that matters.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageBucketInfo))]
    public ValueTask<StorageBucketInfo?> Get(string name)
        => js.Invoke<StorageBucketInfo?>("BitButil.storageBuckets.get", name);

    /// <summary>Lists the names of every bucket this origin has, in alphabetical order.</summary>
    public ValueTask<string[]> Keys() => js.Invoke<string[]>("BitButil.storageBuckets.keys");

    /// <summary>
    /// Deletes a bucket and everything in it - its databases, caches and files.
    /// </summary>
    /// <param name="name">The bucket name.</param>
    /// <returns>True when the call succeeded. Deleting a bucket that isn't there is not an error.</returns>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Delete(string name) => js.Invoke<bool>("BitButil.storageBuckets.delete", name);

    /// <summary>Asks the browser to make this bucket persistent. The user agent decides whether to grant.</summary>
    /// <param name="name">The bucket name.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Persist(string name) => js.Invoke<bool>("BitButil.storageBuckets.persist", name);

    /// <summary>True when this bucket is exempt from eviction.</summary>
    /// <param name="name">The bucket name.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Persisted(string name) => js.Invoke<bool>("BitButil.storageBuckets.persisted", name);

    /// <summary>
    /// This bucket's own quota and usage, in bytes.
    /// </summary>
    /// <param name="name">The bucket name.</param>
    /// <remarks>
    /// <see cref="StorageEstimate.UsageDetails"/> is always empty here - a bucket reports one number
    /// for itself, with no per-API breakdown.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageEstimate))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(StorageUsageDetail))]
    public ValueTask<StorageEstimate> Estimate(string name)
        => js.Invoke<StorageEstimate>("BitButil.storageBuckets.estimate", name);

    /// <summary>
    /// Sets the point after which the browser may delete the bucket.
    /// </summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="expires">When the bucket becomes disposable.</param>
    /// <remarks>
    /// An expiry is permission to delete, not a scheduled deletion: the data usually survives past
    /// it, and reading it back afterwards is not something to rely on.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> SetExpires(string name, DateTimeOffset expires)
        => js.Invoke<bool>("BitButil.storageBuckets.setExpires", name, expires.ToUnixTimeMilliseconds());

    /// <summary>Reads the bucket's expiry, or null when it has none.</summary>
    /// <param name="name">The bucket name.</param>
    public async ValueTask<DateTimeOffset?> GetExpires(string name)
    {
        var expires = await js.Invoke<long?>("BitButil.storageBuckets.getExpires", name);
        return expires is null ? null : DateTimeOffset.FromUnixTimeMilliseconds(expires.Value);
    }

    /// <summary>
    /// Lists a directory inside the bucket's own file system. Not recursive.
    /// </summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="path">A directory path inside the bucket, or empty for its root.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(OpfsEntry))]
    public ValueTask<OpfsEntry[]> List(string name, string path = "")
        => js.Invoke<OpfsEntry[]>("BitButil.storageBuckets.list", name, path);

    /// <summary>Reads a file inside the bucket as text.</summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="path">A file path inside the bucket.</param>
    /// <returns>The text, or null when there is no file there.</returns>
    public ValueTask<string?> ReadText(string name, string path)
        => js.Invoke<string?>("BitButil.storageBuckets.readText", name, path);

    /// <summary>Writes text to a file inside the bucket, creating what's missing along the path.</summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="path">A file path inside the bucket.</param>
    /// <param name="text">The contents to write.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteText(string name, string path, string text)
        => js.Invoke<bool>("BitButil.storageBuckets.write", name, path, text, null);

    /// <summary>Writes bytes to a file inside the bucket, creating what's missing along the path.</summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="path">A file path inside the bucket.</param>
    /// <param name="data">The contents to write.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> WriteBytes(string name, string path, byte[] data)
        => js.Invoke<bool>("BitButil.storageBuckets.write", name, path, null, data);

    /// <summary>Deletes a file or directory inside the bucket.</summary>
    /// <param name="name">The bucket name.</param>
    /// <param name="path">The path inside the bucket to delete.</param>
    /// <param name="recursive">Required to delete a directory that isn't empty.</param>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> Remove(string name, string path, bool recursive = false)
        => js.Invoke<bool>("BitButil.storageBuckets.remove", name, path, recursive);

    private static string? Durability(StorageBucketDurability durability) => durability switch
    {
        StorageBucketDurability.Relaxed => "relaxed",
        StorageBucketDurability.Strict => "strict",
        _ => null,
    };
}
