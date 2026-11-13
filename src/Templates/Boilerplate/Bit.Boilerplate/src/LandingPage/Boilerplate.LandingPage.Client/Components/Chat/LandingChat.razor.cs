using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Boilerplate.LandingPage.Client.Components.Chat;

public partial class LandingChat
{
    // The same files the project file marks with BlazorWebAssemblyLazyLoad
    private static readonly string[] lazyAssemblies = ["Bit.BlazorUI.AI.wasm", "Bit.BlazorUI.AI.Core.wasm", "System.Net.WebSockets.Client.wasm", "System.Net.WebSockets.wasm"];

    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;

    /// <summary>The sample the chat answers about. Without one it's the gallery's chat, which knows every sample.</summary>
    [Parameter] public LandingSite? Site { get; set; }

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
    private Task<bool>? loading;

    private string ChatTitle => Title ?? (Site is null
        ? Localizer[nameof(LandingStrings.ChatGalleryTitle)]
        : Localizer[nameof(LandingStrings.ChatTitle), Site.Name]);

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

    /// <summary>
    /// Starts the download the moment the visitor reaches for the chat - a pointer over its button, the focus in it, a
    /// finger on it - so it's mostly done by the time they click, while a visitor who never chats still downloads none of it.
    /// </summary>
    private void Prepare()
    {
        if (isLoaded is false)
        {
            _ = Load();
        }
    }

    private async Task EnsureLoaded()
    {
        if (isLoaded)
            return;

        isLoading = true;

        isLoaded = await Load();
        hasFailed = isLoaded is false;

        isLoading = false;
    }

    // One download however many times it's asked for, and a failed one is tried again the next time
    private Task<bool> Load()
    {
        if (loading is null || (loading.IsCompleted && loading.Result is false))
        {
            loading = LoadAssets();
        }

        return loading;
    }

    private async Task<bool> LoadAssets()
    {
        try
        {
            // The stylesheet only: the scripts of Bit.BlazorUI.AI come as modules, each the first time the chat calls into it (See BitBlazorUILazyScripts)
            await jsRuntime.InvokeVoidAsync("landing.loadAssets", (string[])["_content/Bit.BlazorUI.AI/styles/bit.blazorui.ai.css"], (string[])[]);

            // Only a non-AOT WebAssembly build marks them lazy; anywhere else they're loaded already
            if (OperatingSystem.IsBrowser() && (AppContext.TryGetSwitch("Boilerplate.IsAot", out var isAot) && isAot) is false)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(lazyAssemblies);
            }

            return true;
        }
        catch (Exception exp) when (exp is JSException or HttpRequestException or InvalidOperationException)
        {
            // Offline, or a deployment the page outlived: the chat says it couldn't open, and opening it again tries again
            return false;
        }
    }
}

public enum LandingChatVariant
{
    Bubble,
    Inline,
    Concierge
}
