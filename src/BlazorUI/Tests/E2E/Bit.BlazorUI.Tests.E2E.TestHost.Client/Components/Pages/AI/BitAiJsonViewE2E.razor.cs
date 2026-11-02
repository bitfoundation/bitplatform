using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Net.Http;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using Bit.BlazorUI;
using Bit.BlazorUI.Tests.E2E.TestHost.Components;
using Bit.BlazorUI.Tests.E2E.TestHost.Components.Shared;
using Bit.BlazorUI.Tests.E2E.TestHost.AI;
using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.TestHost.Components.Pages.AI;

public partial class BitAiJsonViewE2E
{
    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    // Every kind of value, a container one level down (tags, address) and one two levels down (geo).
    private readonly JsonElement _person = Parse("""
        {"name":"Ada","age":36,"admin":true,"manager":null,"tags":["x","y"],"address":{"city":"London","geo":{"lat":51.5}}}
        """);

    private readonly JsonElement _small = Parse("""{"a":{"b":1},"c":[2,3]}""");

    private readonly JsonElement _large = Parse($"[{string.Join(",", Enumerable.Range(1, 30))}]");

    // Two items with the same key, each its own container.
    private readonly JsonElement _twins = Parse("""[{"inner":{"x":1}},{"inner":{"x":2}}]""");

    private readonly JsonElement _nested = Parse("""{"open":1,"closed":{"x":1}}""");

    private int _disabledClicks;
    private int _enabledClicks;
}
