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

public partial class BitAiDataTableE2E
{
    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    // The second row brings a column the first one does not have, and a nested object, a null and the booleans.
    private readonly JsonElement _people = Parse("""
        [
          {"firstName":"Ada","age":36,"remote":true,"address":{"city":"London"},"note":null},
          {"firstName":"Grace","age":45,"remote":false,"tags":["navy","cobol"]}
        ]
        """);

    private readonly JsonElement _object = Parse("""{"name":"Ada"}""");

    private readonly JsonElement _scalarItem = Parse("""[{"name":"Ada"},"stray",{"name":"Grace"}]""");

    private readonly JsonElement _emptyArray = Parse("[]");

    private readonly JsonElement _wide = Parse("""
        [{"alpha":"a long value of the first column","beta":"a long value of the second column","gamma":"a long value of the third column","delta":"a long value of the fourth column"}]
        """);

    private readonly List<string> _growRows = ["""{"name":"Ada"}"""];
    private static readonly string[] MoreRows = ["""{"name":"Grace","role":"Admiral"}""", """{"name":"Linus","role":"Kernel","country":"Finland"}"""];

    private void AddRow()
    {
        if (_growRows.Count - 1 < MoreRows.Length) _growRows.Add(MoreRows[_growRows.Count - 1]);
    }

    private static JsonElement RowsOf(List<string> rows) => Parse($"[{string.Join(",", rows)}]");
}
