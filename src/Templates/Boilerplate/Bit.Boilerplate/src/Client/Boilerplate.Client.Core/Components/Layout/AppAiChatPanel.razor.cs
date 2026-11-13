//+:cnd:noEmit
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Boilerplate.Client.Core.Components.Layout.AiChat;

namespace Boilerplate.Client.Core.Components.Layout;

/// <summary>
/// The AI chat's floating button and panel. The chat itself (<see cref="AppAiChat"/>) is built on Bit.BlazorUI.AI, which
/// the browser downloads only when the chat is first reached for - its assemblies, styles and scripts - so this shell,
/// on every page of the app, holds nothing of it.
/// </summary>
public partial class AppAiChatPanel
{
    /// <summary>The BlazorWebAssemblyLazyLoad items of Client.Web the chat needs; SignalR's are AppHubConnection's own.</summary>
    private static readonly string[] lazyAssemblies = ["Bit.BlazorUI.AI.wasm", "Bit.BlazorUI.AI.Core.wasm", "Bit.BlazorUI.AI.SignalR.wasm"];

    [AutoInject] private LazyAssemblyLoader lazyAssemblyLoader = default!;
    [AutoInject] private SignInModalService signInModalService = default!;

    private bool isOpen;
    private bool isLoaded;
    private bool isLoading;
    private bool hasFailed;
    private bool isSmallScreen;
    private Task<bool>? loading;
    private AppAiChat? chat;
    private string? initialPrompt;
    private AppAiChatStatus status = new(IsInVoiceCall: false, NeedsAnswer: false);

    /// <summary>
    /// The panel is widened to the whole viewport, for a long answer or a wide code block. It outlives a close and a
    /// reopen, the way a maximized window does.
    /// </summary>
    private bool isMaximized;

    //#if(module == "Sales")
    private Action unsubSearchProducts = default!;
    //#endif
    //#if(ads == true)
    private Action unsubAdHaveTrouble = default!;
    //#endif

    protected override Task OnInitAsync()
    {
        //#if(module == "Sales")
        unsubSearchProducts = PubSubService.Subscribe(ClientAppMessages.SEARCH_PRODUCTS, async (value) =>
        {
            if (isOpen) return;

            var message = (string?)value;

            await OpenAndSend(string.IsNullOrWhiteSpace(message) ? Localizer[nameof(AppStrings.AiChatPanelPrompt3)] : message);
        });
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble = PubSubService.Subscribe(ClientAppMessages.AD_HAVE_TROUBLE, async _ =>
        {
            if (isOpen) return;

            await OpenAndSend(Localizer[nameof(AppStrings.UpgradeAdHaveTroublePrompt)]);
        });
        //#endif

        NavigationManager.LocationChanged += NavigationManager_LocationChanged;

        return base.OnInitAsync();
    }

    private async Task Open()
    {
        isOpen = true;

        await EnsureLoaded();
    }

    /// <summary>Opens the chat on a prompt of the app's own, as the user's next message.</summary>
    private async Task OpenAndSend(string prompt)
    {
        var sendNow = false;

        await InvokeAsync(async () =>
        {
            // A chat already there sends it now; one about to be created sends it once it has read its conversation back
            sendNow = chat is not null;
            initialPrompt = sendNow ? null : prompt;

            await Open();

            StateHasChanged();
        });

        if (sendNow)
        {
            await chat!.SendPrompt(prompt);
        }
    }

    /// <summary>Starts the download as the user reaches for the chat, so opening it rarely waits; a visitor who never does downloads none of it.</summary>
    private void Prepare()
    {
        if (isLoaded is false)
        {
            _ = Load();
        }
    }

    private async Task EnsureLoaded()
    {
        if (isLoaded) return;

        isLoading = true;
        hasFailed = false;
        StateHasChanged();

        try
        {
            isLoaded = await Load();
            hasFailed = isLoaded is false;
        }
        finally
        {
            isLoading = false;
        }
    }

    // One download however many times it's asked for, and one that failed - or broke off, as JS interop does when a Blazor
    // Server circuit drops - is tried again the next time
    private Task<bool> Load()
    {
        if (loading is null || (loading.IsCompleted && loading is not { IsCompletedSuccessfully: true, Result: true }))
        {
            loading = LoadAssets();
        }

        return loading;
    }

    private async Task<bool> LoadAssets()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("App.loadAssets",
                                            (string[])["_content/Bit.BlazorUI.AI/styles/bit.blazorui.ai.css"],
                                            (string[])["_content/Bit.BlazorUI.AI/scripts/bit.blazorui.ai.js"]);

            // Only a non-AOT WebAssembly build marks them lazy; anywhere else they're loaded already
            if (AppPlatform.IsBrowser && AppPlatform.IsAot is false)
            {
                await lazyAssemblyLoader.LoadAssembliesAsync(lazyAssemblies);
            }

            return true;
        }
        catch (Exception exp) when (exp is JSException or JSDisconnectedException or TaskCanceledException or HttpRequestException or InvalidOperationException)
        {
            // Offline, a deployment the page outlived or a dropped circuit: the panel says it couldn't open, and the retry
            // tries again
            ExceptionHandler.Handle(exp, ExceptionDisplayKind.None);
            return false;
        }
    }

    private void HandleOnStatusChanged(AppAiChatStatus status)
    {
        this.status = status;
        StateHasChanged();
    }

    /// <summary>
    /// The conversation is open to anyone, but dictation, read aloud and attaching an image each reach an endpoint that
    /// requires a signed-in user. The modal turns a request that would come back 401 into one the user can complete
    /// without leaving the conversation, and the snack bar says why it appeared - landing on top of a conversation, the
    /// modal explains nothing on its own.
    /// </summary>
    private async Task<bool> EnsureSignedIn(string title, string message)
    {
        if ((await AuthenticationStateTask).User.IsAuthenticated()) return true;

        SnackBarService.Info(title, message);

        var wasOpen = isOpen;

        isOpen = false; // Focus on the modal, not the conversation, so the panel is closed to avoid a focus trap.
        StateHasChanged();

        var result = await signInModalService.SignIn();

        isOpen = wasOpen;
        StateHasChanged();

        return result;
    }

    private async Task ClearChat()
    {
        if (chat is not null)
        {
            await chat.Clear();
        }
    }

    /// <summary>Stops only what's read aloud: a voice call and a streaming answer go on with the panel closed.</summary>
    private async Task HandleOnDismissPanel()
    {
        if (chat is not null)
        {
            await chat.OnPanelClosed();
        }
    }

    /// <summary>A page opened from the chat would be hidden behind a panel that covers the screen, so that panel closes.</summary>
    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        if (isOpen is false || (isSmallScreen is false && isMaximized is false)) return;

        _ = InvokeAsync(() =>
        {
            isOpen = false;
            StateHasChanged();
        });
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        //#if(module == "Sales")
        unsubSearchProducts();
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble();
        //#endif

        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;

        await base.DisposeAsync(disposing);
    }
}
