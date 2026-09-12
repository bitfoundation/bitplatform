using System.Text;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders;

namespace Bit.BlazorUI;

public static class BitFileVersionProvider
{
    // Keyed on everything the answer depends on: the file provider the file is read from, the path as
    // written, the query key, and the request path base - but the base only when the path starts with it,
    // which is the only case in which it takes part in the lookup. That keeps the table bounded by the
    // paths an app renders, however many bases it is asked to render them under.
    // An entry carries the file's change token, so a file rewritten in place - dotnet watch, a deploy that
    // swaps the web root - gets a fresh hash, and a file that appears after the first request gets one at all.
    private static readonly ConcurrentDictionary<CacheKey, CacheEntry> PathCache = new();



    public static string AppendFileVersion(IFileProvider fileProvider, PathString requestPathBase, string path, string versionKey = "v")
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentNullException(nameof(path));

        var resolvedPath = path;

        var queryStringOrFragmentStartIndex = path.AsSpan().IndexOfAny('?', '#');
        if (queryStringOrFragmentStartIndex != -1)
        {
            resolvedPath = path[..queryStringOrFragmentStartIndex];
        }

        if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            // Don't append version if the path is absolute.
            return path;
        }

        var pathBase = requestPathBase.HasValue && resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase)
                       ? requestPathBase.Value
                       : string.Empty;

        var key = new CacheKey(fileProvider, pathBase, path, versionKey);

        if (PathCache.TryGetValue(key, out var entry) && entry.Token.HasChanged is false) return entry.Value;

        entry = GenerateVersionedPath(fileProvider, pathBase, path, versionKey, resolvedPath);

        PathCache[key] = entry;

        return entry.Value;
    }



    private static CacheEntry GenerateVersionedPath(IFileProvider fileProvider, string pathBase, string path, string versionKey, string resolvedPath)
    {
        // Watched before it is read, so a change between the two is not missed.
        var token = fileProvider.Watch(resolvedPath);
        var fileInfo = fileProvider.GetFileInfo(resolvedPath);

        if (fileInfo.Exists is false && pathBase.Length > 0)
        {
            var requestPathBaseRelativePath = resolvedPath[pathBase.Length..];

            token = new CompositeChangeToken([token, fileProvider.Watch(requestPathBaseRelativePath)]);
            fileInfo = fileProvider.GetFileInfo(requestPathBaseRelativePath);
        }

        var value = fileInfo.Exists ? AddQueryString(path, versionKey, GenerateFileHash(fileInfo)) : path;

        return new CacheEntry(value, token);
    }

    private static string AddQueryString(string uri, string key, string value)
    {
        var anchorIndex = uri.IndexOf('#');
        var uriToBeAppended = uri.AsSpan();
        var anchorText = ReadOnlySpan<char>.Empty;

        if (anchorIndex != -1)
        {
            anchorText = uriToBeAppended[anchorIndex..];
            uriToBeAppended = uriToBeAppended[..anchorIndex];
        }

        var queryIndex = uriToBeAppended.IndexOf('?');
        var hasQuery = queryIndex != -1;

        var sb = new StringBuilder();

        sb.Append(uriToBeAppended);

        sb.Append(hasQuery ? '&' : '?');
        sb.Append(key);
        sb.Append('=');
        sb.Append(value);

        sb.Append(anchorText);

        return sb.ToString();
    }

    private static string GenerateFileHash(IFileInfo fileInfo)
    {
        using var readStream = fileInfo.CreateReadStream();

        using var sha256 = SHA256.Create();

        var bytes = sha256.ComputeHash(readStream);

        var hash = Convert.ToBase64String(bytes)
                          .Replace("+", "-")
                          .Replace("/", "_")
                          .TrimEnd('=');

        return $"sha256-{Uri.EscapeDataString(hash)}";
    }



    private readonly record struct CacheKey(IFileProvider FileProvider, string PathBase, string Path, string VersionKey);

    private sealed record CacheEntry(string Value, IChangeToken Token);
}
