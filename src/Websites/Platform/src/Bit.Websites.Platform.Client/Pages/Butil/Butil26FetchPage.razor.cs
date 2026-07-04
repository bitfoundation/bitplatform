using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil26FetchPage
{
    private string url = "https://jsonplaceholder.typicode.com/todos/1";
    private string? sendGetStatus;
    private string? sendGetBody;
    private string? progressText;

    private CancellationTokenSource? cts;


    private async Task SendGet()
    {
        sendGetStatus = null;
        sendGetBody = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            sendGetStatus = "Enter a URL first.";
            return;
        }

        try
        {
            var response = await fetch.Send(new FetchRequest { Url = url.Trim() });

            if (response.Error is not null)
            {
                sendGetStatus = $"Error: {response.Error}";
                return;
            }

            sendGetStatus = $"Status {response.Status} {response.StatusText} - {response.Body.Length} bytes from {response.Url}";
            sendGetBody = PreviewBody(response.Body);
        }
        catch (Exception ex)
        {
            sendGetStatus = $"Error: {ex.Message}";
        }
    }

    private bool sending;

    private async Task SendWithProgress()
    {
        // Prevent overlapping downloads from racing on the shared cts field.
        if (sending) return;
        sending = true;

        progressText = null;

        // Cancel and dispose any previous source before replacing it; keep a local
        // reference so this call only ever disposes/clears its own token source.
        cts?.Cancel();
        cts?.Dispose();
        var localCts = cts = new CancellationTokenSource();

        try
        {
            var response = await fetch.Send(
                new FetchRequest { Url = "https://jsonplaceholder.typicode.com/photos" },
                onProgress: p =>
                {
                    progressText = p.Total.HasValue
                        ? $"Received {p.Loaded} / {p.Total} bytes"
                        : $"Received {p.Loaded} bytes";
                    InvokeAsync(StateHasChanged);
                },
                cancellationToken: localCts.Token);

            progressText = response.Aborted ? "Aborted" : $"Complete - {response.Body.Length} bytes";
        }
        catch (Exception ex)
        {
            progressText = $"Error: {ex.Message}";
        }
        finally
        {
            if (ReferenceEquals(cts, localCts)) cts = null;
            localCts.Dispose();
            sending = false;
        }
    }

    private void AbortRequest() => cts?.Cancel();

    private static string PreviewBody(byte[] body)
    {
        if (body.Length == 0) return "(empty body)";
        var text = System.Text.Encoding.UTF8.GetString(body);
        return text.Length > 500 ? text[..500] + "..." : text;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (cts is not null)
        {
            // Cancel first so any in-flight SendWithProgress request aborts the JS fetch
            // (via BitButil.fetch.abort) instead of being left running after teardown.
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        await base.DisposeAsync(disposing);
    }


    private readonly string sendGetExampleCode =
@"@inject Bit.Butil.Fetch fetch

<BitTextField @bind-Value=""url"" Label=""URL"" />

<BitButton OnClick=""SendGet"">Send GET</BitButton>

<div>@sendGetStatus</div>
<pre>@sendGetBody</pre>

@code {
    private string url = ""https://jsonplaceholder.typicode.com/todos/1"";
    private string? sendGetStatus;
    private string? sendGetBody;

    private async Task SendGet()
    {
        var response = await fetch.Send(new FetchRequest { Url = url.Trim() });

        if (response.Error is not null)
        {
            sendGetStatus = $""Error: {response.Error}"";
            return;
        }

        sendGetStatus = $""Status {response.Status} {response.StatusText} - {response.Body.Length} bytes from {response.Url}"";
        sendGetBody = System.Text.Encoding.UTF8.GetString(response.Body);
    }
}";

    private readonly string sendProgressExampleCode =
@"@inject Bit.Butil.Fetch fetch

<BitButton OnClick=""SendWithProgress"">Download with progress</BitButton>
<BitButton OnClick=""AbortRequest"">Abort</BitButton>

<div>@progressText</div>

@code {
    private string? progressText;
    private CancellationTokenSource? cts;

    private async Task SendWithProgress()
    {
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var response = await fetch.Send(
            new FetchRequest { Url = ""https://jsonplaceholder.typicode.com/photos"" },
            onProgress: p =>
            {
                progressText = p.Total.HasValue
                    ? $""Received {p.Loaded} / {p.Total} bytes""
                    : $""Received {p.Loaded} bytes"";
                InvokeAsync(StateHasChanged);
            },
            cancellationToken: cts.Token);

        progressText = response.Aborted ? ""Aborted"" : ""Complete"";
    }

    private void AbortRequest() => cts?.Cancel();
}";
}
