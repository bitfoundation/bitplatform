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

public partial class BitAiMarkdownViewE2E
{
    private const string BasicMarkdown = """
        # Render modes

        Blazor renders in **two places**, and the difference *matters*. ~~Never~~ Sometimes.

        ## Where the code runs

        - Server, over a circuit
        - WebAssembly, in the browser

        3. The third step
        4. The fourth step

        - [x] Measure the first paint
        - [ ] Measure every update

        | Mode | Runs on | First paint |
        | --- | :---: | ---: |
        | Server | The server | Fast |
        | WebAssembly | The browser | Slower |

        Read [the render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes) or call `StateHasChanged`.

        > Rendering more often than the eye can follow is not smoother.

        ---

        ```csharp
        var total = items.Sum(item => item.Price);
        ```
        """;

    private const string HeadingsMarkdown = """
        # Render modes

        The first section.

        ## Server

        The second section.

        # Render modes

        The same heading again.
        """;

    private const string UnsafeMarkdown = """
        [a script link](javascript:alert('a'))

        [an upper-case one](JaVaScRiPt:alert('b'))

        [an encoded one](javascript&#58;alert('c'))

        [a tabbed one](<java&#9;script:alert('d')>)

        [a vbscript one](vbscript:msgbox('e'))

        [a data one](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)

        [a safe one](https://bitplatform.dev/) and [a mail one](mailto:info@bitplatform.dev) and [a relative one](/e2e/aimarkdownview)

        ![a script image](javascript:alert('f'))

        An inline <img src="x" onerror="alert('g')"> and <script>alert('h')</script> written as markup.

        <div onmouseover="alert('i')">An HTML block</div>
        """;

    private static string Images(string prefix) => $"""
        ![A chart of the visitors](https://images.e2e.test/{prefix}/chart.svg)

        ![](https://other.e2e.test/{prefix}/photo.svg)

        ![A pixel](data:image/svg+xml,%3Csvg%20xmlns='http://www.w3.org/2000/svg'%20width='24'%20height='24'%3E%3Crect%20width='24'%20height='24'%20fill='%232563eb'/%3E%3C/svg%3E)
        """;

    private const string RtlMarkdown = """
        برای رندر دوباره `x = 1;` را بنویسید.

        > رندر کردن بیشتر از آنچه چشم می‌بیند روان‌تر نیست.

        - سرور
        - مرورگر
        """;

    private const string VariablesMarkdown = """
        # A styled answer

        A paragraph with [a link](https://bitplatform.dev/) and `inline code`, then a list:

        - a first point
        - a second point

        > A quote.

        | A | B |
        | --- | --- |
        | 1 | 2 |

        ---

        Still being written
        """;

    private static readonly string FootnotesMarkdown =
        "A claim[^1] and another one[^2], and the first again[^1].\n\n" +
        string.Join("\n\n", Enumerable.Range(1, 30).Select(i => $"Paragraph {i} of the text between the references and the notes.")) +
        "\n\n[^1]: The first note.\n[^2]: The second note.";

    private const string Footnotes2Markdown = "Another view with a note of its own[^1].\n\n[^1]: The note of the second view.";

    private readonly BitAiMarkdownDocument _basic = BitAiMarkdownParser.Parse(BasicMarkdown);
    private readonly BitAiMarkdownDocument _headings = BitAiMarkdownParser.Parse(HeadingsMarkdown);
    private readonly BitAiMarkdownDocument _footnotes = BitAiMarkdownParser.Parse(FootnotesMarkdown);
    private readonly BitAiMarkdownDocument _footnotes2 = BitAiMarkdownParser.Parse(Footnotes2Markdown);
    private readonly BitAiMarkdownDocument _unsafe = BitAiMarkdownParser.Parse(UnsafeMarkdown);
    private readonly BitAiMarkdownDocument _imagesAsk = BitAiMarkdownParser.Parse(Images("ask"));
    private readonly BitAiMarkdownDocument _imagesBlock = BitAiMarkdownParser.Parse(Images("block"));
    private readonly BitAiMarkdownDocument _imagesListed = BitAiMarkdownParser.Parse(Images("listed"));
    private readonly BitAiMarkdownDocument _imagesAllow = BitAiMarkdownParser.Parse(Images("allow"));
    private readonly BitAiMarkdownDocument _open = BitAiMarkdownParser.Parse("The answer is still being", isComplete: false);
    private readonly BitAiMarkdownDocument _closed = BitAiMarkdownParser.Parse("The answer is complete.");
    private readonly BitAiMarkdownDocument _rtl = BitAiMarkdownParser.Parse(RtlMarkdown);
    private readonly BitAiMarkdownDocument _variables = BitAiMarkdownParser.Parse(VariablesMarkdown, isComplete: false);

    private readonly BitAiUiOptions _clickToLoad = BitAiUiOptions.Default with { Images = BitAiImagePolicy.ClickToLoad };
    private readonly BitAiUiOptions _blockImages = BitAiUiOptions.Default with { Images = BitAiImagePolicy.Block };
    private readonly BitAiUiOptions _listedHosts = BitAiUiOptions.Default with { Images = BitAiImagePolicy.AllowListedHosts("images.e2e.test") };
    private readonly BitAiUiOptions _noCaret = BitAiUiOptions.Default with { ShowStreamingCaret = false };
}
