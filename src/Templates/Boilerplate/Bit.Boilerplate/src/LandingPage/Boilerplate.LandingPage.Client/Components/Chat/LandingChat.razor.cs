using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.LandingPage.Client.Components.Chat;

public partial class LandingChat
{
    private static readonly string[] lazyAssemblies = ["Bit.BlazorUI.AI.wasm", "System.Net.WebSockets.Client.wasm", "System.Net.WebSockets.wasm"];

    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;

    [Parameter, EditorRequired] public LandingSite Site { get; set; } = default!;

    [Parameter] public LandingChatVariant Variant { get; set; }

    [Parameter] public IReadOnlyList<string>? Suggestions { get; set; }

    [Parameter] public string? Title { get; set; }

    [Parameter] public string? Label { get; set; }

    [Parameter] public string? Placeholder { get; set; }

    [Parameter] public string? HandoffUrl { get; set; }

    [Parameter] public RenderFragment? Footer { get; set; }

    [Parameter] public string? Class { get; set; }

    private bool isOpen;
    private bool isLoaded;
    private bool isLoading;
    private bool hasFailed;
    private string? draft;
    private string? question;

    protected override void OnParametersSet()
    {
        Title ??= Localizer[nameof(LandingStrings.ChatTitle), Site.Name];

        base.OnParametersSet();
    }

    private async Task Toggle()
    {
        if (isOpen)
        {
            isOpen = false;
            return;
        }

        await Open();
    }

    private async Task Open()
    {
        isOpen = true;
        await EnsureLoaded();
    }

    private async Task Ask(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || isLoading)
            return;

        await EnsureLoaded();

        if (isLoaded)
        {
            question = text.Trim();
        }
    }

    private async Task EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoading = true;

        try
        {
            await jsRuntime.InvokeVoidAsync("landing.loadAssets", (string[])["_content/Bit.BlazorUI.AI/styles/bit.blazorui.ai.css"], (string[])["_content/Bit.BlazorUI.AI/scripts/bit.blazorui.ai.js"]);

            if (OperatingSystem.IsBrowser() && (AppContext.TryGetSwitch("Boilerplate.IsAot", out var isAot) && isAot) is false)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(lazyAssemblies);
            }

            isLoaded = true;
            hasFailed = false;
        }
        catch (JSException)
        {
            hasFailed = true;
        }
        finally
        {
            isLoading = false;
        }
    }
}

public enum LandingChatVariant
{
    Bubble,
    Inline,
    Concierge
}
