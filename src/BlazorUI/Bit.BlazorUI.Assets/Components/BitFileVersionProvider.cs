using System.Text;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Bit.BlazorUI;

public static class BitFileVersionProvider
{
    private static readonly ConcurrentDictionary<string, string> _versionCache = new();



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

        return _versionCache.GetOrAdd(path, _ =>
        {
            var fileInfo = fileProvider.GetFileInfo(resolvedPath);

            if (fileInfo.Exists is false &&
                requestPathBase.HasValue &&
                resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
            {
                var requestPathBaseRelativePath = resolvedPath[requestPathBase.Value.Length..];
                fileInfo = fileProvider.GetFileInfo(requestPathBaseRelativePath);
            }

            if (fileInfo.Exists is false) return path;

            var hash = GetFileHash(fileInfo);

            return AddQueryString(path, versionKey, Uri.EscapeDataString(hash));
        });
    }



    private static string GetFileHash(IFileInfo fileInfo)
    {
        using var readStream = fileInfo.CreateReadStream();

        var bytes = SHA256.HashData(readStream);

        return $"sha256-{Convert.ToBase64String(bytes)}";
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
}
