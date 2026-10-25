using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Cli.Infrastructure;

namespace Bit.Cli.AotProfiling;

public sealed class AppReloadedException(string path) : Exception($"{path} reloaded the page.")
{
    public string Path { get; } = path;
}

public sealed partial class ProfileCollector(DevToolsPage page, PublishedApp app, CliLog log)
{
    public const string WriteAt = "System.Runtime.InteropServices.JavaScript.JavaScriptExports::GetManagedStackTrace";
    public const string SendTo = "System.Runtime.InteropServices.JavaScript.JavaScriptExports::DumpAotProfileData";

    private const string InitScript = """
        (() => {
            delete Navigator.prototype.serviceWorker;
            try { performance.setResourceTimingBufferSize(1000000); } catch { }
            const config = { aotProfilerOptions: { writeAt: '__WRITE_AT__', sendTo: '__SEND_TO__' } };
            const wrap = (blazor, start) => options => {
                options ??= {};
                const configureRuntime = options.configureRuntime;
                options.configureRuntime = builder => { configureRuntime?.(builder); builder.withConfig(config); };
                options.webAssembly ??= {};
                const configureWebAssemblyRuntime = options.webAssembly.configureRuntime;
                options.webAssembly.configureRuntime = builder => { configureWebAssemblyRuntime?.(builder); builder.withConfig(config); };
                return start.call(blazor, options);
            };
            let blazor;
            Object.defineProperty(window, 'Blazor', {
                configurable: true,
                get: () => blazor,
                set: value => {
                    blazor = value;
                    if (!value) return;
                    let start = typeof value.start === 'function' ? wrap(value, value.start) : undefined;
                    Object.defineProperty(value, 'start', { configurable: true, get: () => start, set: f => start = typeof f === 'function' ? wrap(value, f) : f });
                }
            });
        })();
        """;

    private const string WaitForRuntime = """
        async timeout => {
            const end = Date.now() + timeout;
            while (Date.now() < end) {
                if (typeof globalThis.getDotnetRuntime === 'function' && globalThis.getDotnetRuntime(0)) return true;
                await new Promise(r => setTimeout(r, 250));
            }
            return false;
        }
        """;

    private const string Settle = """
        async () => {
            let changes = 0;
            const observer = new MutationObserver(() => changes++);
            observer.observe(document, { subtree: true, childList: true, characterData: true });
            const resources = () => performance.getEntriesByType('resource').length;
            const started = performance.now();
            let seen = resources(), seenChanges = 0, quietSince = started, tick = started;
            try {
                while (performance.now() - started < 30000) {
                    await new Promise(r => setTimeout(r, 100));
                    const now = performance.now(), busy = now - tick > 300;
                    tick = now;
                    if (busy || resources() !== seen || changes !== seenChanges || document.readyState !== 'complete') {
                        seen = resources();
                        seenChanges = changes;
                        quietSince = now;
                    } else if (now - quietSince >= 1500) {
                        break;
                    }
                }
            } finally {
                observer.disconnect();
            }
            return location.pathname;
        }
        """;

    private const string Links = """
        () => [...document.querySelectorAll('a[href]')]
            .filter(a => !a.hasAttribute('download') && (!a.target || a.target === '_self'))
            .map(a => a.href)
        """;

    private const string Navigate = """
        path => {
            if (window.__bitAotProfile !== 1) return 'reloaded';
            if (!window.Blazor?.navigateTo) return 'no-blazor';
            window.Blazor.navigateTo(path);
            return 'ok';
        }
        """;

    private const string ElementCenter = """
        selector => {
            const element = document.querySelector(selector);
            if (!element) return null;
            element.scrollIntoView({ block: 'center', inline: 'center' });
            const box = element.getBoundingClientRect();
            return { x: box.left + box.width / 2, y: box.top + box.height / 2 };
        }
        """;

    private const string Write = """
        async () => {
            const runtime = getDotnetRuntime(0);
            const early = !!runtime.INTERNAL.aotProfileData;
            if (!early) {
                if (typeof DotNet === 'undefined') return 'no-dotnet';
                try { DotNet.invokeMethod('Bit.Cli.AotProfile', 'Write'); } catch (e) { String(e.stack); }
            }
            for (let i = 0; i < 150 && !runtime.INTERNAL.aotProfileData; i++) await new Promise(r => setTimeout(r, 100));
            return runtime.INTERNAL.aotProfileData ? (early ? 'early' : 'written') : 'missing';
        }
        """;

    private const string Read = """
        () => {
            const bytes = getDotnetRuntime(0).INTERNAL.aotProfileData;
            let text = '';
            for (let i = 0; i < bytes.length; i += 0x8000) text += String.fromCharCode.apply(null, bytes.subarray(i, i + 0x8000));
            return btoa(text);
        }
        """;

    private readonly List<string> errors = [];

    public IReadOnlyList<string> Errors => errors;

    public bool WrittenEarly { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        page.EventReceived += OnEvent;

        await page.SendAsync("Page.enable", cancellationToken: cancellationToken);
        await page.SendAsync("Runtime.enable", cancellationToken: cancellationToken);
        await page.SendAsync("Page.setBypassCSP", new { enabled = true }, cancellationToken);
        await page.SendAsync("Page.addScriptToEvaluateOnNewDocument", new { source = InitScript.Replace("__WRITE_AT__", WriteAt).Replace("__SEND_TO__", SendTo) }, cancellationToken);
        await page.SendAsync("Fetch.enable", new
        {
            patterns = app.IsStatic
                ? new object[] { new { urlPattern = app.Origin + "*", requestStage = "Request" } }
                : [new { urlPattern = "*", resourceType = "Document", requestStage = "Response" }]
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> VisitAsync(string startPath, int maxPages, IReadOnlyCollection<string> skipped, Action<string> progress, CancellationToken cancellationToken)
    {
        await page.SendAsync("Page.navigate", new { url = new Uri(app.Origin, startPath).ToString() }, cancellationToken);

        if ((await page.EvaluateAsync(WaitForRuntime, 180_000, cancellationToken)).ValueKind is not JsonValueKind.True)
            throw new InvalidOperationException("The app's .NET runtime didn't start within 3 minutes.");

        await page.EvaluateAsync("() => { window.__bitAotProfile = 1; }", cancellationToken: cancellationToken);
        var visited = new List<string> { (await page.EvaluateAsync(Settle, cancellationToken: cancellationToken)).GetString()! };
        progress(visited[0]);
        var queue = new Queue<string>();

        while (visited.Count < maxPages)
        {
            foreach (var link in (await page.EvaluateAsync(Links, cancellationToken: cancellationToken)).EnumerateArray())
            {
                if (PagePath(link.GetString(), app.Origin) is { } path && visited.Contains(path) is false && queue.Contains(path) is false && skipped.Contains(path) is false)
                {
                    queue.Enqueue(path);
                }
            }

            if (queue.TryDequeue(out var next) is false)
                break;

            switch ((await page.EvaluateAsync(Navigate, next, cancellationToken)).GetString())
            {
                case "reloaded":
                    throw new AppReloadedException(visited[^1]);
                case "no-blazor":
                    return visited;
            }

            await page.EvaluateAsync(Settle, cancellationToken: cancellationToken);
            visited.Add(next);
            progress(next);
        }

        if ((await page.EvaluateAsync("() => window.__bitAotProfile === 1", cancellationToken: cancellationToken)).ValueKind is not JsonValueKind.True)
            throw new AppReloadedException(visited[^1]);

        return visited;
    }

    public async Task<bool> RunStepAsync(string step, CancellationToken cancellationToken)
    {
        if (step.StartsWith('/'))
        {
            await page.EvaluateAsync(Navigate, step, cancellationToken);
        }
        else if (step.StartsWith("click:", StringComparison.Ordinal))
        {
            var center = await page.EvaluateAsync(ElementCenter, step["click:".Length..], cancellationToken);
            if (center.ValueKind is not JsonValueKind.Object)
                return false;

            var (x, y) = (center.GetProperty("x").GetDouble(), center.GetProperty("y").GetDouble());
            await page.SendAsync("Input.dispatchMouseEvent", new { type = "mouseMoved", x, y }, cancellationToken);
            await page.SendAsync("Input.dispatchMouseEvent", new { type = "mousePressed", x, y, button = "left", clickCount = 1 }, cancellationToken);
            await page.SendAsync("Input.dispatchMouseEvent", new { type = "mouseReleased", x, y, button = "left", clickCount = 1 }, cancellationToken);
        }
        else if (step.StartsWith("key:", StringComparison.Ordinal))
        {
            var key = step["key:".Length..];
            var (code, text) = KeyCode(key);
            await page.SendAsync("Input.dispatchKeyEvent", new { type = "keyDown", key, text, windowsVirtualKeyCode = code }, cancellationToken);
            await page.SendAsync("Input.dispatchKeyEvent", new { type = "keyUp", key, windowsVirtualKeyCode = code }, cancellationToken);
        }
        else if (step.StartsWith("wait:", StringComparison.Ordinal))
        {
            await Task.Delay(int.Parse(step["wait:".Length..]), cancellationToken);
        }

        await page.EvaluateAsync(Settle, cancellationToken: cancellationToken);

        if ((await page.EvaluateAsync("() => window.__bitAotProfile === 1", cancellationToken: cancellationToken)).ValueKind is not JsonValueKind.True)
            throw new AppReloadedException(step);

        return true;
    }

    public async Task<byte[]> WriteAsync(CancellationToken cancellationToken)
    {
        var result = (await page.EvaluateAsync(Write, cancellationToken: cancellationToken)).GetString();

        switch (result)
        {
            case "no-dotnet":
                throw new InvalidOperationException("The page has no DotNet object: bit aot-profile works with Blazor WebAssembly apps.");
            case "missing":
                throw new InvalidOperationException("The app didn't hand over a profile. Check that it runs on WebAssembly in the browser, not on the server.");
        }

        WrittenEarly = result == "early";
        return Convert.FromBase64String((await page.EvaluateAsync(Read, cancellationToken: cancellationToken)).GetString()!);
    }

    public static bool IsValidStep(string step)
    {
        return step.StartsWith('/')
            || (step.StartsWith("click:", StringComparison.Ordinal) && step.Length > "click:".Length)
            || (step.StartsWith("key:", StringComparison.Ordinal) && step.Length > "key:".Length)
            || (step.StartsWith("wait:", StringComparison.Ordinal) && int.TryParse(step["wait:".Length..], out var wait) && wait >= 0);
    }

    public static string? PagePath(string? href, Uri origin)
    {
        if (Uri.TryCreate(href, UriKind.Absolute, out var uri) is false || uri.Scheme != origin.Scheme || string.Equals(uri.Authority, origin.Authority, StringComparison.OrdinalIgnoreCase) is false)
            return null;

        var path = uri.AbsolutePath;
        return path.TrimEnd('/').Split('/')[^1].Contains('.') ? null : path;
    }

    public static string StartBlazorManually(string html)
    {
        var script = BlazorScript().Match(html);
        if (script.Success is false || script.Value.Contains("autostart", StringComparison.OrdinalIgnoreCase))
            return html;

        var close = html.IndexOf("</script>", script.Index + script.Length, StringComparison.OrdinalIgnoreCase);
        if (close < 0)
            return html;

        var end = close + "</script>".Length;
        return string.Concat(html[..script.Index], script.Value[..^1], " autostart=\"false\">", html[(script.Index + script.Length)..end], "<script>Blazor.start();</script>", html[end..]);
    }

    public static (int Status, string ContentType, byte[] Body) ServeStatic(string webRoot, Uri url, bool document)
    {
        var root = Path.GetFullPath(webRoot);
        var file = Path.GetFullPath(Path.Combine(root, Uri.UnescapeDataString(url.AbsolutePath).TrimStart('/')));

        if (file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) is false || File.Exists(file) is false)
        {
            if (document is false)
                return (404, "text/plain", Encoding.UTF8.GetBytes("Not found"));

            file = Path.Combine(root, "index.html");
        }

        var contentType = ContentType(file);
        return (200, contentType, contentType.StartsWith("text/html", StringComparison.Ordinal)
            ? Encoding.UTF8.GetBytes(StartBlazorManually(File.ReadAllText(file)))
            : File.ReadAllBytes(file));
    }

    private static string ContentType(string file) => Path.GetExtension(file).ToLowerInvariant() switch
    {
        ".html" or ".htm" => "text/html; charset=utf-8",
        ".js" or ".mjs" => "text/javascript",
        ".css" => "text/css",
        ".json" => "application/json",
        ".webmanifest" => "application/manifest+json",
        ".wasm" => "application/wasm",
        ".svg" => "image/svg+xml",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".ico" => "image/x-icon",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        ".txt" => "text/plain",
        ".xml" => "application/xml",
        _ => "application/octet-stream"
    };

    private static (int Code, string? Text) KeyCode(string key) => key switch
    {
        "Enter" => (13, "\r"),
        "Escape" => (27, null),
        "Tab" => (9, null),
        "Backspace" => (8, null),
        "ArrowLeft" => (37, null),
        "ArrowUp" => (38, null),
        "ArrowRight" => (39, null),
        "ArrowDown" => (40, null),
        "/" => (191, "/"),
        { Length: 1 } => (char.ToUpperInvariant(key[0]), key),
        _ => (0, null)
    };

    private void OnEvent(string method, JsonElement parameters)
    {
        switch (method)
        {
            case "Fetch.requestPaused":
                _ = Task.Run(() => HandleRequestAsync(parameters));
                break;
            case "Runtime.exceptionThrown":
                var details = parameters.GetProperty("exceptionDetails");
                var text = details.TryGetProperty("exception", out var exception) && exception.TryGetProperty("description", out var description)
                    ? description.GetString() ?? ""
                    : details.GetProperty("text").GetString() ?? "";
                log.Write($"page error: {text}");
                lock (errors)
                {
                    if (errors.Count < 20)
                        errors.Add(text.Split('\n')[0]);
                }
                break;
            case "Runtime.consoleAPICalled" when parameters.GetProperty("type").GetString() is "error" or "warning":
                log.Write($"console {parameters.GetProperty("type").GetString()}: {string.Join(' ', parameters.GetProperty("args").EnumerateArray().Select(a => a.TryGetProperty("value", out var value) ? value.ToString() : a.TryGetProperty("description", out var d) ? d.GetString() : ""))}");
                break;
        }
    }

    private async Task HandleRequestAsync(JsonElement paused)
    {
        var requestId = paused.GetProperty("requestId").GetString();

        try
        {
            if (paused.TryGetProperty("responseStatusCode", out var status))
            {
                var response = await page.SendAsync("Fetch.getResponseBody", new { requestId });
                var body = response.GetProperty("body").GetString() ?? "";
                var html = response.GetProperty("base64Encoded").GetBoolean() ? Encoding.UTF8.GetString(Convert.FromBase64String(body)) : body;
                var headers = paused.TryGetProperty("responseHeaders", out var original)
                    ? original.EnumerateArray()
                        .Select(h => new { name = h.GetProperty("name").GetString()!, value = h.GetProperty("value").GetString()! })
                        .Where(h => h.name.Equals("content-length", StringComparison.OrdinalIgnoreCase) is false && h.name.Equals("content-encoding", StringComparison.OrdinalIgnoreCase) is false)
                        .ToArray()
                    : [];

                await page.SendAsync("Fetch.fulfillRequest", new { requestId, responseCode = status.GetInt32(), responseHeaders = headers, body = Convert.ToBase64String(Encoding.UTF8.GetBytes(StartBlazorManually(html))) });
                return;
            }

            var url = new Uri(paused.GetProperty("request").GetProperty("url").GetString()!);
            var (code, contentType, content) = ServeStatic(app.WebRoot, url, paused.GetProperty("resourceType").GetString() == "Document");
            await page.SendAsync("Fetch.fulfillRequest", new
            {
                requestId,
                responseCode = code,
                responseHeaders = new[] { new { name = "Content-Type", value = contentType }, new { name = "Cache-Control", value = "no-store" } },
                body = Convert.ToBase64String(content)
            });
        }
        catch (Exception exp) when (exp is DevToolsException or IOException or UnauthorizedAccessException or FormatException)
        {
            log.Write($"request {requestId}: {exp.Message}");

            try
            {
                await page.SendAsync("Fetch.continueRequest", new { requestId });
            }
            catch (DevToolsException)
            {
            }
        }
    }

    [GeneratedRegex("""<script\b[^>]*\bsrc\s*=\s*["'][^"']*_framework/blazor\.(?:web|webassembly)(?:\.[a-z0-9]+)?\.js["'][^>]*>""", RegexOptions.IgnoreCase)]
    private static partial Regex BlazorScript();
}
