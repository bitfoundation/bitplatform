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

namespace Bit.BlazorUI.Tests.E2E.TestHost.Components.Pages.AI;

public partial class BitAiCodeBlockViewE2E
{
    // Line feeds whatever the line endings of this file are on disk, so what a test reads back is exact.
    private const string CSharpCode = "// Adds two numbers\npublic static int Add(int a, int b)\n{\n    var name = \"total\";\n    return a + b + 42;\n}";

    private const string ShortCode = "return a + b;";

    private const string JsonCode = """{ "name": "bit", "count": 3, "on": true }""";

    private const string DiffCode = "--- a/file.cs\n+++ b/file.cs\n-var x = 1;\n+var x = 2;";

    private const string HtmlCode = """<a href="/home" class="link">Home</a>""";

    private const string RustCode = "fn longest<'a>(x: &'a str) -> &'a str { x }";

    private const string YamlCode = "name: bit\ncount: 3";

    private const string RtlMarkdown = "این کد را اجرا کنید:\n\n```csharp\nreturn a + b;\n```";

    private static readonly string LongLine = "var veryLongExpression = " + string.Join(" + ", Enumerable.Range(1, 40).Select(i => $"value{i}")) + ";";

    private readonly BitAiUiOptions _numbered = BitAiUiOptions.Default with { ShowLineNumbers = true };
    private readonly BitAiUiOptions _noCopy = BitAiUiOptions.Default with { ShowCodeCopy = false };

    private string _streamCode = "public static int Add(";
    private bool _streaming = true;

    private void Append() => _streamCode += "int a, int b) => a + b;";
}
