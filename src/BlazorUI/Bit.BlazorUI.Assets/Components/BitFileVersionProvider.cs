using System.Web;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Bit.BlazorUI;

public class BitFileVersionProvider(IFileProvider fileProvider)
{
    public string AppendFileVersion(PathString requestPathBase, string path, string versionKey = "v")
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

        var separator = path.Contains('?') ? "&" : "?";

        return $"{path}{separator}{versionKey}={HttpUtility.UrlEncode(hash)}";
    }

    private static string GetFileHash(IFileInfo fileInfo)
    {
        using var readStream = fileInfo.CreateReadStream();

        var bytes = SHA256.HashData(readStream);

        return Convert.ToBase64String(bytes)
                      .TrimEnd('=')
                      .Replace('+', '-')
                      .Replace('/', '_');
    }
}
