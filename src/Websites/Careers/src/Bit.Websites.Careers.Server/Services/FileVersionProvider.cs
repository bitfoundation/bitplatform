using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;

namespace Bit.Websites.Careers.Server.Services;

public partial class FileVersionProvider
{
    private const string VersionKey = "assetVer";
    private static readonly char[] QueryStringAndFragmentTokens = ['?', '#'];

    [AutoInject] private readonly IWebHostEnvironment webHostEnv = default!;
    [AutoInject] private readonly IMemoryCache cache = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;

    public string AddFileVersionToPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var resolvedPath = path;

        var queryStringOrFragmentStartIndex = path.IndexOfAny(QueryStringAndFragmentTokens);
        if (queryStringOrFragmentStartIndex != -1)
        {
            resolvedPath = path[..queryStringOrFragmentStartIndex];
        }

        if (Uri.TryCreate(resolvedPath, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            // Don't append version if the path is absolute.
            return path;
        }

        var requestPathBase = httpContextAccessor.HttpContext!.Request.PathBase;

        return cache.GetOrCreate(path, cacheEntryOptions =>
        {
            cacheEntryOptions.AddExpirationToken(webHostEnv.WebRootFileProvider.Watch(resolvedPath));
            var fileInfo = webHostEnv.WebRootFileProvider.GetFileInfo(resolvedPath);

            if (fileInfo.Exists is false &&
                requestPathBase.HasValue &&
                resolvedPath.StartsWith(requestPathBase.Value, StringComparison.OrdinalIgnoreCase))
            {
                var requestPathBaseRelativePath = resolvedPath.Substring(requestPathBase.Value.Length);
                cacheEntryOptions.AddExpirationToken(webHostEnv.WebRootFileProvider.Watch(requestPathBaseRelativePath));
                fileInfo = webHostEnv.WebRootFileProvider.GetFileInfo(requestPathBaseRelativePath);
            }

            if (fileInfo.Exists)
            {
                return QueryHelpers.AddQueryString(path, VersionKey, GetHashForFile(fileInfo));
            }
            else
            {
                // if the file is not in the current server.
                return path;
            }
        })!;
    }

    private static string GetHashForFile(IFileInfo fileInfo)
    {
        using var sha256 = SHA256.Create();
        using var readStream = fileInfo.CreateReadStream();
        var hash = sha256.ComputeHash(readStream);
        return WebEncoders.Base64UrlEncode(hash);
    }
}
