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

public partial class BitAiArtifactViewE2E : IAsyncDisposable
{
    // A page whose script writes what it could and could not do into its own document, for the tests to read through the
    // frame: it runs, its origin is opaque, it cannot touch the page around it nor read its cookies, and a value it draws
    // once tells whether the frame was created again.
    private const string HtmlPage = """
        <!doctype html>
        <html>
        <body>
          <p id="ran">not run</p>
          <p id="origin"></p>
          <p id="parent">untried</p>
          <p id="cookie">untried</p>
          <p id="stamp"></p>
          <button id="counter">Clicked 0 times</button>
          <script>
            document.getElementById('ran').textContent = 'ran';
            document.getElementById('origin').textContent = String(self.origin);
            document.getElementById('stamp').textContent = String(Math.random());
            try {
              window.parent.document.body.setAttribute('data-frame-reached', 'yes');
              document.getElementById('parent').textContent = 'reached';
            } catch (e) {
              document.getElementById('parent').textContent = 'blocked:' + e.name;
            }
            let cookie = '';
            try { cookie = document.cookie; } catch (e) { cookie = ''; }
            document.getElementById('cookie').textContent = cookie.indexOf('e2e-render-mode') >= 0 ? 'read' : 'none';
            let clicks = 0;
            const button = document.getElementById('counter');
            button.onclick = () => button.textContent = 'Clicked ' + (++clicks) + ' times';
          </script>
        </body>
        </html>
        """;

    private const string GreeterCode = "public sealed class Greeter\n{\n    public string Greet(string name) => $\"Hello, {name}!\";\n}\n";

    private const string ReleaseNotes = "# Release notes\n\n- **Artifacts** keep a version history.\n- Structured output fills in while it streams.\n";

    private const string SettingsJson = """{"theme":"dark","fontSize":14,"autosave":true,"recent":["README.md","Program.cs"],"proxy":null}""";

    private const string PricesCsv = "product,price,stock\n\"Keyboard, wireless\",89.50,12\nMouse,20,140\n\"Monitor 27\"\"\",329,4\n";

    private const string RegionsTsv = "region\tsales\nEurope\t1.2M\nAsia\t1.9M\n";

    private const string TeamJson = """[{"name":"Ada","role":"Engineer"},{"name":"Grace","role":"Architect"}]""";

    // A 1x1 PNG.
    private const string PixelPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    // The script would mark the page around it if it ever ran; an image never runs it.
    private const string BadgeSvg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 120 40" width="120" height="40"><rect width="120" height="40" fill="#0065ef" /><script>parent.document.body.setAttribute('data-svg-ran', 'yes');</script></svg>""";

    private readonly BitAiArtifact _code = Completed(BitAiArtifactKind.Code, "Greeter.cs", GreeterCode, "csharp");
    private readonly BitAiArtifact _document = Completed(BitAiArtifactKind.Document, "Release notes", ReleaseNotes, "markdown");
    private readonly BitAiArtifact _html = Completed(BitAiArtifactKind.Html, "Counter page", HtmlPage);
    private readonly BitAiArtifact _json = Completed(BitAiArtifactKind.Json, "Settings", SettingsJson);
    private readonly BitAiArtifact _csv = Completed(BitAiArtifactKind.Table, "Prices", PricesCsv, "csv");
    private readonly BitAiArtifact _tsv = Completed(BitAiArtifactKind.Table, "Regions", RegionsTsv, "tsv");
    private readonly BitAiArtifact _jsonTable = Completed(BitAiArtifactKind.Table, "Team", TeamJson);
    private readonly BitAiArtifact _image = Completed(BitAiArtifactKind.Image, "A pixel", null, url: PixelPng);
    private readonly BitAiArtifact _svg = Completed(BitAiArtifactKind.Svg, "Badge", BadgeSvg);

    private readonly BitAiArtifact _revised = Revised();
    private readonly BitAiArtifact _revisedForInitial = Revised();
    private readonly BitAiArtifact _revisedForDisabled = Revised();

    private readonly BitAiArtifact _fullCode = Completed(BitAiArtifactKind.Code, "Full.cs", GreeterCode, "csharp");
    private readonly BitAiArtifact _fullHtml = Completed(BitAiArtifactKind.Html, "Running page", HtmlPage);
    private readonly BitAiArtifact _modalCode = Completed(BitAiArtifactKind.Code, "Modal.cs", GreeterCode, "csharp");

    private readonly BitAiArtifact _rtlCode = Completed(BitAiArtifactKind.Code, "Rtl.cs", GreeterCode, "csharp");
    private readonly BitAiArtifact _rtlInheritedCode = Completed(BitAiArtifactKind.Code, "Inherited.cs", GreeterCode, "csharp");

    private readonly BitAiArtifact _varsCode = Completed(BitAiArtifactKind.Code, "Vars.cs", GreeterCode + GreeterCode + GreeterCode, "csharp");
    private readonly BitAiArtifact _varsHtml = Completed(BitAiArtifactKind.Html, "Vars page", "<p>A page</p>");
    private readonly BitAiArtifact _varsDocument = Completed(BitAiArtifactKind.Document, "Vars notes", "Some notes.", "markdown");
    private readonly BitAiArtifact _varsCsv = Completed(BitAiArtifactKind.Table, "Vars table", "a,b\n1,2\n", "csv");

    private bool _modalOpen;

    // The artifact script of the E2E transport: a README streamed in, revised into a second version on the next turn.
    // Paced, so the streaming state of the panel can be seen.
    private readonly BitAiChatSession _streamSession = new(E2EAiTransport.For(E2EAiScripts.Artifact, 400));
    private BitAiRenderScheduler? _scheduler;

    protected override void OnInitialized()
    {
        _scheduler = new(() => InvokeAsync(StateHasChanged));
        _streamSession.StateChanged += _scheduler.Request;
    }

    private Task WriteAsync() => _streamSession.SubmitAsync("Write the README");

    private Task ReviseAsync() => _streamSession.SubmitAsync("Add an install step");

    private static int CompletedAnswers(BitAiChatSession session) =>
        session.Conversation.Messages.Count(m => m.Role.IsAssistant && m.Status == BitAiMessageStatus.Complete);

    // A finished artifact, the way a session leaves one once the transport has completed it.
    private static BitAiArtifact Completed(BitAiArtifactKind kind, string title, string? content, string? language = null, string? url = null)
    {
        var artifact = new BitAiArtifact(null, kind, title) { Language = language, Url = url };
        if (content is not null) artifact.SetContent(content);
        artifact.Complete();
        return artifact;
    }

    // Three versions of one file.
    private static BitAiArtifact Revised()
    {
        var artifact = new BitAiArtifact(null, BitAiArtifactKind.Code, "pricing.ts") { Language = "typescript" };

        artifact.SetContent("export const price = 10;");
        artifact.Complete("First draft");

        artifact.BeginRevision();
        artifact.SetContent("export const price = (quantity: number) => quantity * 10;");
        artifact.Complete("Priced per unit");

        artifact.BeginRevision();
        artifact.SetContent("export const price = (quantity: number) => quantity * (quantity >= 10 ? 9 : 10);");
        artifact.Complete("A bulk discount");

        return artifact;
    }

    public async ValueTask DisposeAsync()
    {
        if (_scheduler is not null) await _scheduler.DisposeAsync();

        await _streamSession.DisposeAsync();
    }
}
