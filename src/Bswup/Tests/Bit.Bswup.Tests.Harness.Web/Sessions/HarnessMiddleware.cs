using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;

namespace Bit.Bswup.Tests.Harness.Web;

/// <summary>
/// Runs ahead of everything else in the host and plays the deployment a test needs:
/// <list type="bullet">
/// <item><c>/_harness/*</c> - the control endpoints (never offline, never faulted, never logged);</item>
/// <item>the network going away (<see cref="HarnessSession.Offline"/>) and per-request faults;</item>
/// <item>the files a deployment changes: service-worker.js (generated per session), service-worker-assets.js
/// (versioned per session), harness-version.txt, and the standalone app's index.html (its script-tag options).</item>
/// </list>
/// </summary>
public sealed partial class HarnessMiddleware(RequestDelegate next, HarnessSessions sessions, IWebHostEnvironment environment, IConfiguration configuration)
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly FileExtensionContentTypeProvider _contentTypes = new();

    private readonly string _mode = HarnessRenderModes.FromConfiguration(configuration);

    public async Task InvokeAsync(HttpContext context)
    {
        var session = sessions.Resolve(context);
        context.Items[typeof(HarnessSession)] = session;

        var path = context.Request.Path.Value ?? "/";

        if (path.StartsWith("/_harness/", StringComparison.OrdinalIgnoreCase))
        {
            await HandleControlAsync(context, session, path);
            return;
        }

        var record = session.Record(context);

        if (session.Offline)
        {
            // Still recorded (with status 0): which requests an offline page could not do without is the point of most offline tests.
            context.Abort();
            return;
        }

        try
        {
            if (await ApplyFaultAsync(context, session, record)) return;
            if (await TryServeDeploymentFileAsync(context, session, path)) return;

            // A hosted client's referenced libraries are served from the host's root _content/, while the
            // standalone app - mounted under /standalone/ - resolves them (and lists them in its manifest)
            // relative to its base. Map them the way a reverse proxy mounting the app on a sub-path would.
            if (path.StartsWith("/standalone/_content/", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Path = path["/standalone".Length..];
            }

            await next(context);
        }
        finally
        {
            if (record.Status == 0 && context.RequestAborted.IsCancellationRequested is false)
            {
                record.Status = context.Response.StatusCode;
            }
        }
    }

    private async Task HandleControlAsync(HttpContext context, HarnessSession session, string path)
    {
        var request = context.Request;
        var command = path["/_harness/".Length..].TrimEnd('/').ToLowerInvariant();

        switch (command, request.Method)
        {
            case ("options", "PUT"):
                session.Options = await request.ReadFromJsonAsync<HarnessSessionOptions>(_json) ?? new();
                break;

            case ("options", "GET"):
                await context.Response.WriteAsJsonAsync(session.Options, _json);
                return;

            case ("version", "PUT"):
                session.Version = (await request.ReadFromJsonAsync<VersionBody>(_json))!.Version;
                break;

            case ("offline", "PUT"):
                session.Offline = (await request.ReadFromJsonAsync<OfflineBody>(_json))!.Offline;
                break;

            case ("faults", "POST"):
                session.AddFault((await request.ReadFromJsonAsync<HarnessFault>(_json))!);
                break;

            case ("faults", "DELETE"):
                session.ClearFaults();
                break;

            case ("requests", "GET"):
                await context.Response.WriteAsJsonAsync(session.Requests, _json);
                return;

            case ("requests", "DELETE"):
                session.ClearRequests();
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
        }

        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private async Task<bool> ApplyFaultAsync(HttpContext context, HarnessSession session, HarnessRequestRecord record)
    {
        var fault = session.TakeFault(context.Request.Path.Value + context.Request.QueryString.Value);
        if (fault is null) return false;

        if (fault.DelayMs > 0)
        {
            try
            {
                await Task.Delay(fault.DelayMs, context.RequestAborted);
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        if (fault.Abort)
        {
            context.Abort();
            return true;
        }

        if (fault.Status is { } status)
        {
            context.Response.StatusCode = status;
            context.Response.Headers.CacheControl = "no-store";
            record.Status = status;
            return true;
        }

        if (fault.Corrupt)
        {
            var file = environment.WebRootFileProvider.GetFileInfo(context.Request.Path.Value!.TrimStart('/'));
            if (file.Exists is false)
                throw new InvalidOperationException($"A Corrupt fault matched {context.Request.Path}, which is not a file of the web root.");

            await using var stream = file.CreateReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            // Same length, different bytes: a tampered copy that no length check would notice.
            var bytes = buffer.ToArray();
            bytes[bytes.Length / 2] ^= 0x01;

            await WriteAsync(context, bytes, ContentTypeOf(file.Name));
            return true;
        }

        // A delay-only fault: the request then proceeds normally.
        return false;
    }

    private async Task<bool> TryServeDeploymentFileAsync(HttpContext context, HarnessSession session, string path)
    {
        if (HttpMethods.IsGet(context.Request.Method) is false && HttpMethods.IsHead(context.Request.Method) is false) return false;

        if (path.Equals("/api/ping", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsJsonAsync(new { server = true, version = session.Version }, _json);
            return true;
        }

        foreach (var app in HostedApp.All)
        {
            if (path.Equals(app.PathBase + "/service-worker.js", StringComparison.OrdinalIgnoreCase))
            {
                if (session.Options.Worker == HarnessWorkers.Missing)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return true;
                }

                await WriteTextAsync(context, WorkerScript.Build(session, app, _mode), "text/javascript");
                return true;
            }

            if (path.Equals(app.PathBase + "/service-worker-assets.js", StringComparison.OrdinalIgnoreCase))
            {
                await WriteTextAsync(context, await VersionedManifestAsync(app, session.Version), "text/javascript");
                return true;
            }

            if (path.Equals(app.PathBase + "/harness-version.txt", StringComparison.OrdinalIgnoreCase))
            {
                await WriteTextAsync(context, $"v{session.Version}", "text/plain");
                return true;
            }
        }

        // The standalone app's document: index.html itself (the worker precaches it) and every route under it.
        if (path.Equals("/standalone", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/standalone/");
            return true;
        }

        if (path.StartsWith("/standalone/", StringComparison.OrdinalIgnoreCase)
            && (path.Equals("/standalone/index.html", StringComparison.OrdinalIgnoreCase) || Path.HasExtension(path) is false))
        {
            var app = HostedApp.Standalone;
            var html = await ReadWebRootTextAsync(app.WebRootFolder + "index.html");
            var attributes = string.Join(' ', app.ScriptAttributes(session.Options)
                .Select(a => $"{a.Key}=\"{HtmlEncoder.Default.Encode(a.Value)}\""));

            await WriteTextAsync(context, html.Replace("data-harness-attributes", attributes, StringComparison.Ordinal), "text/html");
            return true;
        }

        return false;
    }

    /// <summary>
    /// The manifest the build generated, with its version suffixed for every deployment after the first. The
    /// asset hashes stay as built - what a release that only changes the version (or files outside the
    /// manifest) looks like - so an update has to migrate the cached assets instead of downloading them again.
    /// </summary>
    private async Task<string> VersionedManifestAsync(HostedApp app, int version)
    {
        var manifest = await ReadWebRootTextAsync(app.WebRootFolder + "service-worker-assets.js");

        if (app.WebRootFolder.Length > 0)
        {
            // The build lists a hosted client's assets under its StaticWebAssetBasePath ("standalone/_framework/..."),
            // while a service worker resolves manifest URLs against its own folder - which already is /standalone/.
            // Served the way an app published into a sub-folder lists them: relative to that folder.
            manifest = manifest.Replace($"\"url\": \"{app.WebRootFolder}", "\"url\": \"", StringComparison.Ordinal);
        }

        if (version <= 1) return manifest;

        return ManifestVersion().Replace(manifest, m => $"{m.Groups["prefix"].Value}{m.Groups["value"].Value}-h{version}\"", 1);
    }

    private async Task<string> ReadWebRootTextAsync(string relativePath)
    {
        var file = environment.WebRootFileProvider.GetFileInfo(relativePath);
        if (file.Exists is false)
            throw new FileNotFoundException($"The harness host cannot find {relativePath} in its web root (was the client built?).", relativePath);

        await using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static Task WriteTextAsync(HttpContext context, string text, string contentType) =>
        WriteAsync(context, Encoding.UTF8.GetBytes(text), $"{contentType}; charset=utf-8");

    private static async Task WriteAsync(HttpContext context, byte[] body, string contentType)
    {
        // Revalidated on every request, as a deployment should serve these: the service-worker scripts must
        // never be answered from the HTTP cache, and neither must a file a test is about to change.
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.ContentType = contentType;
        context.Response.ContentLength = body.Length;

        if (HttpMethods.IsHead(context.Request.Method)) return;

        await context.Response.Body.WriteAsync(body);
    }

    private static string ContentTypeOf(string fileName) =>
        _contentTypes.TryGetContentType(fileName, out var contentType) ? contentType : "application/octet-stream";

    [GeneratedRegex("(?<prefix>\"version\"\\s*:\\s*\")(?<value>[^\"]*)\"")]
    private static partial Regex ManifestVersion();

    private sealed record VersionBody(int Version);

    private sealed record OfflineBody(bool Offline);
}
