using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Cli.AotProfiling;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;

namespace Bit.Cli.Tests;

[TestClass]
public class AotProfileTests
{
    private static readonly AotProfileImage CoreLib = new("System.Private.CoreLib", "0E1B2C3D-0000-0000-0000-000000000000");
    private static readonly AotProfileImage App = new("App", "11111111-0000-0000-0000-000000000000");

    [TestMethod]
    public void Profile_Should_ReadWhatMonosProfilerWrites()
    {
        var data = new MonoProfileWriter()
            .Image(0, "System.Private.CoreLib")
            .Type(1, image: 0, instance: -1, "System.Int32")
            .Image(2, "App")
            .Instance(3, 1)
            .Type(4, image: 2, instance: 3, "App.Services.Cache`1")
            .Method(5, type: 4, instance: -1, "Get", 1, "!0(string)")
            .Type(6, image: 2, instance: -1, "App.Pages.Home/<OnInitializedAsync>d__0")
            .Method(7, type: 6, instance: -1, "MoveNext", 0, "void()")
            .Method(8, type: 1, instance: 3, "Parse", 1, "int(string)")
            .ToArray();

        var profile = AotProfile.Read(data);

        Assert.HasCount(3, profile.Methods);
        Assert.AreEqual("App.Services.Cache`1", profile.Methods[0].Class.Name);
        Assert.AreEqual("System.Int32", profile.Methods[0].Class.Arguments!.Single().Name);
        Assert.AreEqual("App.Pages.Home/<OnInitializedAsync>d__0", profile.Methods[1].Class.Name);
        Assert.AreEqual("void()", profile.Methods[1].Signature);
        Assert.AreEqual("System.Int32", profile.Methods[2].Arguments!.Single().Name);
    }

    [TestMethod]
    public void Profile_Should_ReadBackWhatItWrites()
    {
        var int32 = new AotProfileClass(CoreLib, "System.Int32");
        var profile = new AotProfile();
        profile.Add(new AotProfileMethod(new AotProfileClass(App, "App.Services.Cache`1", [int32]), "Get", 1, "!0(string)"));
        profile.Add(new AotProfileMethod(new AotProfileClass(App, ".Program"), "Main", 1, "void(string[])"));
        profile.Add(new AotProfileMethod(new AotProfileClass(CoreLib, "System.Array"), "Empty", 0, "!!0[]()", [int32]));

        var read = AotProfile.Read(profile.ToArray());

        CollectionAssert.AreEqual(profile.Methods.Select(m => m.Key).ToList(), read.Methods.Select(m => m.Key).ToList());
    }

    [TestMethod]
    public void Profile_Should_AddEachMethodOnce()
    {
        var profile = new AotProfile();
        var main = new AotProfileMethod(new AotProfileClass(App, ".Program"), "Main", 1, "void(string[])");

        Assert.AreEqual(1, profile.AddRange([main, main with { Class = new AotProfileClass(App, ".Program") }]));
        Assert.HasCount(1, profile.Methods);
    }

    [TestMethod]
    public void Profile_Should_RejectAFileThatIsNotOne()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => AotProfile.Read(Encoding.ASCII.GetBytes("MZ not a profile at all")));
    }

    [TestMethod]
    public void UnmanagedCallersOnlyMethods_Should_BeListedTheWayMonoNamesThem()
    {
        var methods = AssemblyMethods.Read(typeof(ProfileSamples).Assembly.Location, unmanagedCallersOnly: true);

        var callback = methods.Single(m => m.Class.Name == "Bit.Cli.Tests.ProfileSamples");
        Assert.AreEqual("Callback", callback.Name);
        Assert.AreEqual("int(int,intptr)", callback.Signature);
        Assert.AreEqual(2, callback.ParameterCount);
        Assert.AreEqual("Bit.Cli.Tests", callback.Class.Image.Name);
    }

    [TestMethod]
    public void EveryMethod_Should_BeListedWithMonosSignatures()
    {
        var methods = AssemblyMethods.Read(typeof(ProfileSamples).Assembly.Location, unmanagedCallersOnly: false)
            .Where(m => m.Class.Name.StartsWith("Bit.Cli.Tests.ProfileSamples", StringComparison.Ordinal))
            .ToDictionary(m => $"{m.Class.Name}::{m.Name}", m => m.Signature);

        Assert.AreEqual("void(string,int&,System.Collections.Generic.List`1<int>[],Bit.Cli.Tests.ProfileSamples/Nested/Inner)", methods["Bit.Cli.Tests.ProfileSamples::Plain"]);
        Assert.AreEqual("void()", methods["Bit.Cli.Tests.ProfileSamples/Nested/Inner::.ctor"]);
        Assert.IsFalse(methods.ContainsKey("Bit.Cli.Tests.ProfileSamples::Generic"));
    }

    [TestMethod]
    [DataRow("""<script src="_framework/blazor.web.js"></script>""", """<script src="_framework/blazor.web.js" autostart="false"></script><script>Blazor.start();</script>""")]
    [DataRow("""<body><script src="_framework/blazor.webassembly.4rb4qnk8jm.js"></script><script src="app.js"></script>""", """<body><script src="_framework/blazor.webassembly.4rb4qnk8jm.js" autostart="false"></script><script>Blazor.start();</script><script src="app.js"></script>""")]
    [DataRow("""<script src="_framework/blazor.webassembly.js" autostart="false"></script>""", """<script src="_framework/blazor.webassembly.js" autostart="false"></script>""")]
    [DataRow("""<script src="app.js"></script>""", """<script src="app.js"></script>""")]
    public void TheHostPage_Should_StartBlazorItself(string html, string expected)
    {
        Assert.AreEqual(expected, ProfileCollector.StartBlazorManually(html));
    }

    [TestMethod]
    [DataRow("http://localhost:5000/settings", "/settings")]
    [DataRow("http://localhost:5000/products/42?page=2#reviews", "/products/42")]
    [DataRow("http://localhost:5000/", "/")]
    [DataRow("http://localhost:5000/files/report.pdf", null)]
    [DataRow("https://localhost:5000/settings", null)]
    [DataRow("http://example.com/settings", null)]
    [DataRow("mailto:someone@example.com", null)]
    public void Links_Should_LeadOnlyToPagesOfTheApp(string href, string? expected)
    {
        Assert.AreEqual(expected, ProfileCollector.PagePath(href, new Uri("http://localhost:5000/")));
    }

    [TestMethod]
    [DataRow("/settings", true)]
    [DataRow("click:.menu button", true)]
    [DataRow("key:/", true)]
    [DataRow("wait:500", true)]
    [DataRow("wait:soon", false)]
    [DataRow("click:", false)]
    [DataRow("settings", false)]
    public void Steps_Should_BeChecked(string step, bool valid)
    {
        Assert.AreEqual(valid, ProfileCollector.IsValidStep(step));
    }

    [TestMethod]
    public void AStaticApp_Should_BeServedFromItsFolder()
    {
        using var host = new TestHost();
        var root = Directory.CreateDirectory(Path.Combine(host.Root, "wwwroot")).FullName;
        File.WriteAllText(Path.Combine(root, "index.html"), """<script src="_framework/blazor.webassembly.js"></script>""");
        Directory.CreateDirectory(Path.Combine(root, "_framework"));
        File.WriteAllBytes(Path.Combine(root, "_framework", "dotnet.native.wasm"), [0, 97, 115, 109]);
        File.WriteAllText(Path.Combine(host.Root, "secret.txt"), "secret");

        var wasm = ProfileCollector.ServeStatic(root, new Uri("http://localhost:5000/_framework/dotnet.native.wasm"), document: false);
        var page = ProfileCollector.ServeStatic(root, new Uri("http://localhost:5000/settings/profile"), document: true);
        var missing = ProfileCollector.ServeStatic(root, new Uri("http://localhost:5000/missing.js"), document: false);
        var outside = ProfileCollector.ServeStatic(root, new Uri("http://localhost:5000/%2E%2E/secret.txt"), document: false);

        Assert.AreEqual((200, "application/wasm"), (wasm.Status, wasm.ContentType));
        CollectionAssert.AreEqual(new byte[] { 0, 97, 115, 109 }, wasm.Body);
        Assert.AreEqual(200, page.Status);
        StringAssert.Contains(Encoding.UTF8.GetString(page.Body), "Blazor.start();");
        Assert.AreEqual(404, missing.Status);
        Assert.AreEqual(404, outside.Status);
    }

    [TestMethod]
    public void PlaywrightsChromium_Should_BeFoundBeforeTheInstalledBrowsers()
    {
        using var host = new TestHost();
        var playwright = Path.Combine(host.Root, "ms-playwright");
        string[] chromium = host.Environment.Os switch
        {
            HostOs.Windows => ["chromium-1248", "chrome-win64", "chrome.exe"],
            HostOs.MacOS => ["chromium-1248", "chrome-mac-arm64", "Google Chrome for Testing.app", "Contents", "MacOS", "Google Chrome for Testing"],
            _ => ["chromium-1248", "chrome-linux64", "chrome"]
        };
        var executable = Path.Combine([playwright, .. chromium]);
        Directory.CreateDirectory(Path.GetDirectoryName(executable)!);
        File.WriteAllText(executable, "");
        var older = Path.Combine([playwright, "chromium-1200", .. chromium[1..]]);
        Directory.CreateDirectory(Path.GetDirectoryName(older)!);
        File.WriteAllText(older, "");

        var environment = new CliEnvironment
        {
            Os = host.Environment.Os,
            Architecture = host.Environment.Architecture,
            HomeDirectory = host.Home,
            CurrentDirectory = host.WorkingDirectory,
            Variables = new Dictionary<string, string>(host.Environment.Variables, StringComparer.OrdinalIgnoreCase) { ["PLAYWRIGHT_BROWSERS_PATH"] = playwright }
        };

        Assert.AreEqual(executable, Chromium.Find(environment, _ => "/usr/bin/google-chrome"));
    }

    [TestMethod]
    public async Task AFolderWithoutAProject_Should_BeRefused()
    {
        using var host = new TestHost();

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("aot-profile"));
        StringAssert.Contains(host.Output, "No project here");
    }

    [TestMethod]
    public async Task AFailedPublish_Should_ShowItsErrors()
    {
        using var host = new TestHost();
        var project = Path.Combine(host.WorkingDirectory, "App.Client.csproj");
        File.WriteAllText(project, "<Project />");
        var browser = Path.Combine(host.Root, "chrome.exe");
        File.WriteAllText(browser, "");
        host.Runner.On("dotnet", "publish", 1, $"{project}(3,1): error CS1002: ; expected");

        Assert.AreEqual(CliApp.ExitFailed, await host.RunAsync("aot-profile", "--browser", browser, "-p:InvariantGlobalization=true", "-p:RunAOTCompilation=true"));

        StringAssert.Contains(host.FlatOutput, "error CS1002");
        var arguments = host.Runner.Calls.Single().Arguments.ToList();
        Assert.IsGreaterThan(arguments.IndexOf("-p:RunAOTCompilation=true"), arguments.IndexOf("-p:RunAOTCompilation=false"));
        CollectionAssert.IsSubsetOf(AotProfileRun.ProfilerProperties.Select(p => $"-p:{p}").ToList(), arguments);
    }

    [TestMethod]
    public async Task AnUnknownStep_Should_BeRefusedBeforePublishing()
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "App.Client.csproj"), "<Project />");

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("aot-profile", "--step", "press the button"));

        StringAssert.Contains(host.Output, "isn't a step");
        Assert.IsEmpty(host.Runner.Calls);
    }

    [TestMethod]
    public async Task AnUnknownInclude_Should_BeRefusedBeforeTheAppIsUsed()
    {
        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "App.Client.csproj"), "<Project />");
        var browser = Path.Combine(host.Root, "chrome.exe");
        File.WriteAllText(browser, "");
        PublishesAStaticApp(host);

        Assert.AreEqual(CliApp.ExitUsage, await host.RunAsync("aot-profile", "--browser", browser, "--include", "Bit.Cli.Tests", "--include", "Bit.Bmage"), host.Output);

        StringAssert.Contains(host.FlatOutput, "--include Bit.Bmage: the app has no assembly by that name.");
    }

    [TestMethod]
    public async Task APageThatReloadsWhileItSettles_Should_BeLeftOut()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "App.Client.csproj"), "<Project />");
        PublishesAStaticApp(host);
        await using var browser = new FakeBrowser(host.Root) { ReloadsWhileSettling = "/sign-out" };
        browser.Links["/"] = ["/counter", "/sign-out"];

        Assert.AreEqual(0, await host.RunAsync("aot-profile", "--browser", browser.Executable), host.Output);

        StringAssert.Contains(host.FlatOutput, "/sign-out reloads the app");
        StringAssert.Contains(host.FlatOutput, "Used the app 2 pages");
        Assert.IsTrue(File.Exists(Path.Combine(host.WorkingDirectory, "App.Client.aotprofile")));
    }

    [TestMethod]
    public async Task AStepThatReloads_Should_BeLeftOutWhenTheAppIsUsedAgain()
    {
        if (OperatingSystem.IsWindows() is false)
            return;

        using var host = new TestHost();
        File.WriteAllText(Path.Combine(host.WorkingDirectory, "App.Client.csproj"), "<Project />");
        PublishesAStaticApp(host);
        await using var browser = new FakeBrowser(host.Root) { ReloadingClick = "#sign-out" };

        var run = host.RunAsync("aot-profile", "--browser", browser.Executable, "--step", "click:#sign-out", "--step", "wait:1");

        Assert.AreSame(run, await Task.WhenAny(run, Task.Delay(TimeSpan.FromMinutes(1))), "aot-profile keeps running the step that reloads the app.");
        Assert.AreEqual(0, await run, host.Output);
        Assert.AreEqual(1, browser.Clicks);
        StringAssert.Contains(host.FlatOutput, "click:#sign-out reloads the app");
        StringAssert.Contains(host.FlatOutput, "Used the app 1 page, 1 step");
    }

    private static void PublishesAStaticApp(TestHost host)
    {
        host.Runner.On("dotnet", "publish", spec =>
        {
            var framework = Directory.CreateDirectory(Path.Combine(spec.Arguments[spec.Arguments.ToList().IndexOf("-o") + 1], "wwwroot", "_framework")).FullName;
            File.WriteAllText(Path.Combine(framework, "..", "index.html"), "<html></html>");
            File.Copy(typeof(AotProfileTests).Assembly.Location, Path.Combine(framework, "Bit.Cli.Tests.dll"));
            return new ProcessResult { ExitCode = 0 };
        });
    }

    private sealed class MonoProfileWriter
    {
        private readonly MemoryStream stream = new();
        private readonly BinaryWriter writer;

        public MonoProfileWriter()
        {
            writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Encoding.ASCII.GetBytes("AOTPROFILE"));
            writer.Write(1 << 16);
        }

        public MonoProfileWriter Image(int id, string name) => Record(1, id, () => { String(name); String("00000000-0000-0000-0000-000000000000"); });

        public MonoProfileWriter Type(int id, int image, int instance, string name) => Record(2, id, () => { writer.Write((byte)0x12); writer.Write(image); writer.Write(instance); String(name); });

        public MonoProfileWriter Instance(int id, params int[] types) => Record(3, id, () => { writer.Write(types.Length); Array.ForEach(types, writer.Write); });

        public MonoProfileWriter Method(int id, int type, int instance, string name, int parameters, string signature)
            => Record(4, id, () => { writer.Write(type); writer.Write(instance); writer.Write(parameters); String(name); String(signature); });

        public byte[] ToArray()
        {
            Record(0, 0, () => { });
            return stream.ToArray();
        }

        private MonoProfileWriter Record(byte type, int id, Action payload)
        {
            writer.Write(type);
            writer.Write(id);
            payload();
            writer.Flush();
            return this;
        }

        private void String(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }
    }

    private sealed class FakeBrowser : IAsyncDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly string stop;
        private readonly Task serving;
        private TcpClient? client;
        private string page = "/";
        private bool started;

        public FakeBrowser(string folder)
        {
            listener.Start();
            stop = Path.Combine(folder, "browser-stop");
            Executable = Path.Combine(folder, "chrome.cmd");
            File.WriteAllText(Executable, $"""
                @echo off
                :next
                if "%~1"=="" goto wait
                if "%~1"=="--user-data-dir" (
                  > "%~2\DevToolsActivePort" echo {((IPEndPoint)listener.LocalEndpoint).Port}
                  >> "%~2\DevToolsActivePort" echo /devtools/browser/fake
                )
                shift
                goto next
                :wait
                if exist "{stop}" exit /b
                ping -n 2 127.0.0.1 > nul
                goto wait
                """.ReplaceLineEndings("\r\n"));
            serving = Task.Run(ServeAsync);
        }

        public string Executable { get; }

        public Dictionary<string, string[]> Links { get; } = [];

        public string? ReloadsWhileSettling { get; init; }

        public string? ReloadingClick { get; init; }

        public int Clicks { get; private set; }

        public async ValueTask DisposeAsync()
        {
            File.WriteAllText(stop, "");
            client?.Dispose();
            listener.Stop();
            await serving;
        }

        private async Task ServeAsync()
        {
            try
            {
                client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var handshake = new StringBuilder();
                var buffer = new byte[1 << 20];

                while (handshake.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal) is false && await stream.ReadAsync(buffer.AsMemory(0, 1)) > 0)
                {
                    handshake.Append((char)buffer[0]);
                }

                var key = Regex.Match(handshake.ToString(), @"Sec-WebSocket-Key:\s*(\S+)", RegexOptions.IgnoreCase).Groups[1].Value;
                var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
                await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n"));

                using var socket = WebSocket.CreateFromStream(stream, new WebSocketCreationOptions { IsServer = true });

                while (true)
                {
                    var count = 0;
                    ValueWebSocketReceiveResult received;

                    do
                    {
                        received = await socket.ReceiveAsync(buffer.AsMemory(count), CancellationToken.None);
                        count += received.Count;
                    }
                    while (received.EndOfMessage is false);

                    if (received.MessageType is WebSocketMessageType.Close)
                        return;

                    var request = JsonNode.Parse(buffer.AsSpan(0, count))!;
                    var response = new JsonObject { ["id"] = request["id"]!.GetValue<int>() };

                    try
                    {
                        response["result"] = Answer(request["method"]!.GetValue<string>(), request["params"]);
                    }
                    catch (DevToolsException exp)
                    {
                        response["error"] = new JsonObject { ["code"] = -32000, ["message"] = exp.Message };
                    }

                    await socket.SendAsync(Encoding.UTF8.GetBytes(response.ToJsonString()), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception exp) when (exp is IOException or SocketException or WebSocketException or ObjectDisposedException)
            {
            }
        }

        private JsonObject Answer(string method, JsonNode? parameters)
        {
            switch (method)
            {
                case "Target.createTarget":
                    return new JsonObject { ["targetId"] = "page" };
                case "Target.attachToTarget":
                    return new JsonObject { ["sessionId"] = "session" };
                case "Browser.close":
                    File.WriteAllText(stop, "");
                    return [];
                case "Page.navigate":
                    page = new Uri(parameters!["url"]!.GetValue<string>()).AbsolutePath;
                    return [];
                case "Runtime.evaluate":
                    var expression = parameters!["expression"]!.GetValue<string>();
                    var call = expression.LastIndexOf(")(", StringComparison.Ordinal);
                    var argument = expression[(call + 2)..^1];
                    return new JsonObject { ["result"] = Evaluate(expression[1..call], argument.Length == 0 ? null : JsonNode.Parse(argument)) is { } value ? new JsonObject { ["value"] = value } : [] };
                default:
                    return [];
            }
        }

        private JsonNode? Evaluate(string function, JsonNode? argument)
        {
            if (function.Contains("getDotnetRuntime === 'function'", StringComparison.Ordinal))
                return true;

            if (function.Contains("window.__bitAotProfile = 1", StringComparison.Ordinal))
            {
                started = true;
                return null;
            }

            if (function.Contains("navigateTo", StringComparison.Ordinal))
            {
                if (started is false)
                    return "reloaded";

                page = argument!.GetValue<string>();
                return "ok";
            }

            if (function.Contains("MutationObserver", StringComparison.Ordinal))
            {
                if (page == ReloadsWhileSettling)
                {
                    started = false;
                    throw new DevToolsException("Execution context was destroyed.");
                }

                return page;
            }

            if (function.Contains("a[href]", StringComparison.Ordinal))
                return new JsonArray([.. Links.GetValueOrDefault(page, []).Select(link => JsonValue.Create($"http://localhost:5000{link}"))]);

            if (function.Contains("scrollIntoView", StringComparison.Ordinal))
            {
                Clicks++;
                started &= argument!.GetValue<string>() != ReloadingClick;
                return new JsonObject { ["x"] = 10, ["y"] = 10 };
            }

            if (function.Contains("window.__bitAotProfile === 1", StringComparison.Ordinal))
                return started;

            if (function.Contains("invokeMethod", StringComparison.Ordinal))
                return "written";

            if (function.Contains("btoa", StringComparison.Ordinal))
                return Convert.ToBase64String(new MonoProfileWriter().Image(0, "App").Type(1, image: 0, instance: -1, "App.Program").Method(2, type: 1, instance: -1, "Main", 0, "void()").ToArray());

            return null;
        }
    }
}

public static class ProfileSamples
{
    [UnmanagedCallersOnly]
    public static int Callback(int value, nint context) => value + (int)context;

    public static void Plain(string text, ref int count, List<int>[] lists, Nested.Inner inner) => count += text.Length + lists.Length + inner.GetHashCode();

    public static T Generic<T>(T value) => value;

    public static class Nested
    {
        public sealed class Inner;
    }
}
