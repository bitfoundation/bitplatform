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

public partial class BitAiFormE2E
{
    private const string MainSchema = """
        {
          "title": "Book a demo",
          "description": "Tell us who you are and we will be in touch.",
          "type": "object",
          "required": ["name", "email", "plan", "tags", "address"],
          "properties": {
            "name": { "type": "string", "title": "Name", "minLength": 2, "description": "Your full name." },
            "email": { "type": "string", "format": "email", "title": "Email" },
            "age": { "type": "integer", "title": "Age", "minimum": 18, "maximum": 120 },
            "plan": { "type": "string", "title": "Plan", "enum": ["free", "pro", "team"], "enumNames": ["Free", "Pro", "Team"] },
            "subscribe": { "type": "boolean", "title": "Subscribe to the newsletter" },
            "notes": { "type": "string", "format": "textarea", "title": "Notes" },
            "address": {
              "type": "object",
              "title": "Address",
              "required": ["city"],
              "properties": {
                "city": { "type": "string", "title": "City" },
                "zip": { "type": "string", "title": "Zip", "pattern": "^[0-9]{5}$" }
              }
            },
            "tags": { "type": "array", "title": "Tags", "minItems": 1, "maxItems": 3, "items": { "type": "string" } }
          }
        }
        """;

    private const string SmallSchema = """
        {
          "title": "Rename the file",
          "type": "object",
          "required": ["fileName"],
          "properties": {
            "fileName": { "type": "string", "title": "File name" }
          }
        }
        """;

    private BitAiFormDefinition _main = default!;
    private BitAiFormDefinition _prefilled = default!;
    private BitAiFormDefinition _small = default!;
    private BitAiFormDefinition _noCancel = default!;

    private int _submitCount;
    private int _cancelCount;
    private string _values = "-";
    private string _json = "-";
    private string _prefillJson = "-";
    private int _busyCount;
    private int _disabledCount;

    protected override void OnInitialized()
    {
        _main = BitAiFormDefinition.FromJsonSchema(Parse(MainSchema));
        _prefilled = BitAiFormDefinition.FromJsonSchema(Parse(MainSchema),
                                                        Parse("""{ "name": "Ada Lovelace", "age": 36, "plan": "pro", "subscribe": true, "tags": ["math", "engines"], "address": { "city": "London" } }"""));
        _small = BitAiFormDefinition.FromJsonSchema(Parse(SmallSchema)) with { SubmitLabel = "Rename" };
        _noCancel = _small with { CancelLabel = string.Empty, Title = "No cancel" };
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private void HandleSubmit(IReadOnlyDictionary<string, string?> values)
    {
        _submitCount++;
        _values = string.Join(";", values.Where(v => string.IsNullOrEmpty(v.Value) is false)
                                         .OrderBy(v => v.Key, StringComparer.Ordinal)
                                         .Select(v => $"{v.Key}={v.Value}"));
    }

    private void HandleSubmitJson(JsonElement json) => _json = json.GetRawText();
}
