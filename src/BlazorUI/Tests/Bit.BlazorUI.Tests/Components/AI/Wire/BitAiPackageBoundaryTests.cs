using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.AI.Wire;

/// <summary>
/// Pins what each AI package's assembly references, so nothing an app did not install reaches it: no server type in an
/// assembly a browser or a Hybrid app loads (where the ASP.NET Core shared framework does not exist, and a type built on
/// it fails to load), no UI on a server, and Microsoft.Extensions.AI and the SignalR client only in the packages named
/// after them.
/// </summary>
/// <remarks>
/// The direct references of each compiled assembly, read off the assembly the test run loads: a new <c>using</c> of a
/// forbidden assembly compiles in the package's own project (which may still reference it through another package) and
/// fails here.
/// </remarks>
[TestClass]
public class BitAiPackageBoundaryTests
{
    // What only an ASP.NET Core server has: its shared framework's hosting, routing, HTTP and SignalR server.
    private static readonly string[] ServerOnly =
    [
        "Microsoft.AspNetCore.Http", "Microsoft.AspNetCore.Http.Abstractions", "Microsoft.AspNetCore.Http.Features",
        "Microsoft.AspNetCore.Routing", "Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.SignalR.Core",
        "Microsoft.AspNetCore.SignalR", "Microsoft.AspNetCore.WebSockets", "Microsoft.AspNetCore.Server.Kestrel.Core",
        "Microsoft.AspNetCore.Mvc.Core",
    ];

    private static readonly string[] Ui = ["Microsoft.AspNetCore.Components", "Microsoft.AspNetCore.Components.Web", "Microsoft.JSInterop", "Bit.BlazorUI", "Bit.BlazorUI.AI"];

    private static readonly string[] ExtensionsAI = ["Microsoft.Extensions.AI", "Microsoft.Extensions.AI.Abstractions"];

    private static readonly string[] SignalRClient = ["Microsoft.AspNetCore.SignalR.Client", "Microsoft.AspNetCore.SignalR.Client.Core"];

    private static readonly string[] Satellites = ["Bit.BlazorUI.AI.Server", "Bit.BlazorUI.AI.SignalR", "Bit.BlazorUI.AI.ChatClient"];

    [TestMethod]
    public void TheCoreReferencesNoUiNoServerAndNoOtherPackage() =>
        AssertReferencesNone(typeof(BitAiChatSession).Assembly, [.. ServerOnly, .. Ui, .. ExtensionsAI, .. SignalRClient, .. Satellites]);

    [TestMethod]
    public void TheUiReferencesNoServerNoExtensionsAIAndNoSignalR() =>
        AssertReferencesNone(typeof(BitAiChat).Assembly, [.. ServerOnly, .. ExtensionsAI, .. SignalRClient, .. Satellites]);

    [TestMethod]
    public void TheServerReferencesNoUiNoExtensionsAIAndNoSignalRClient() =>
        AssertReferencesNone(typeof(BitAiHub).Assembly, [.. Ui, .. ExtensionsAI, .. SignalRClient, .. Satellites]);

    [TestMethod]
    public void TheSignalRTransportReferencesNoServerNoUiAndNoExtensionsAI() =>
        AssertReferencesNone(typeof(BitAiSignalRTransport).Assembly, [.. ServerOnly, .. Ui, .. ExtensionsAI, .. Satellites]);

    [TestMethod]
    public void TheChatClientAdapterReferencesNoServerNoUiAndNoSignalR() =>
        AssertReferencesNone(typeof(BitAiChatClientTransport).Assembly, [.. ServerOnly, .. Ui, .. SignalRClient, .. Satellites]);

    private static void AssertReferencesNone(Assembly assembly, string[] forbidden)
    {
        var name = assembly.GetName().Name;
        var offending = assembly.GetReferencedAssemblies()
            .Select(r => r.Name!)
            .Where(r => r != name && forbidden.Contains(r, StringComparer.Ordinal))
            .ToArray();

        Assert.AreEqual(0, offending.Length, $"{name} references {string.Join(", ", offending)}, which an app installing it must not be made to load.");
    }
}
