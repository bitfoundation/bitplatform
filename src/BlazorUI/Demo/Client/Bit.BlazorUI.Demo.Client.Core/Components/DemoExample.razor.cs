namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class DemoExample
{
    private bool showCode;

    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string Id { get; set; } = default!;
    [Parameter] public string RazorCode { get; set; } = default!;
    [Parameter] public string CsharpCode { get; set; } = default!;
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;
    [CascadingParameter(Name = nameof(RenderForMcpClient))] public bool RenderForMcpClient { get; set; }

    [Parameter] public bool PreventRenderForMcpClient { get; set; }

    // The collapsed code panel stays rendered so it can animate shut, which would otherwise turn the
    // page-wide highlight pass below into O(examples²) work. Highlighting only this example's own
    // container keeps it linear. Falls back to null - and so to the whole document, exactly as
    // before - for the rare DemoExample declared without an Id.
    private string? _codeElementId => Id.HasValue() ? $"{Id}-code" : null;

    protected override async Task OnInitAsync()
    {
        showCode = RenderForMcpClient;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoid("highlightSnippet", _codeElementId);
    }



    private void ToggleCode() => showCode = !showCode;



    private string AppendCodePhraseToCsharpCode()
    {
        string code = $@"{"\n\n"}@code {{
{CsharpCode.Trim().Replace("\n", "\n\t")}
}}";
        return code;
    }

    // Both copy buttons confirm by swapping their icon for a checkmark and growing into an inline
    // "copied" label; the title doubles as the accessible name for the same confirmation.
    private bool isCodeCopied = false;
    private string codeIcon = BitIconName.Copy;
    private string copyCodeMessage = "Copy code";
    private async Task CopyCodeToClipboard()
    {
        var code = string.IsNullOrEmpty(CsharpCode) is false
                    ? AppendCodePhraseToCsharpCode()
                    : "";

        await JSRuntime.CopyToClipboard(RazorCode.Trim() + code);

        codeIcon = BitIconName.CheckMark;
        copyCodeMessage = "Code copied!";
        isCodeCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isCodeCopied = false;
        codeIcon = BitIconName.Copy;
        copyCodeMessage = "Copy code";

        StateHasChanged();
    }

    private bool isLinkCopied = false;
    private string linkIcon = BitIconName.Link;
    private string copyLinkMessage = "Copy link";
    private async Task CopyLinkToClipboard()
    {
        var currentUrl = NavigationManager.Uri;
        currentUrl = currentUrl.Contains("#") ? currentUrl.Substring(0, currentUrl.IndexOf("#")) : currentUrl;
        var exampleUrl = $"{currentUrl}#{Id}";
        await JSRuntime.CopyToClipboard(exampleUrl);

        linkIcon = BitIconName.CheckMark;
        copyLinkMessage = "Link copied!";
        isLinkCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isLinkCopied = false;
        linkIcon = BitIconName.Link;
        copyLinkMessage = "Copy link";

        StateHasChanged();
    }
}
