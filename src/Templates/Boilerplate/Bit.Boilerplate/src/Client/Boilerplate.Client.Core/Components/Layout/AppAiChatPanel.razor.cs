//+:cnd:noEmit
using System.Threading.Channels;
using Boilerplate.Shared.Dtos.Chatbot;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppAiChatPanel
{
    private bool isOpen;
    private bool isLoading;
    private string? userInput;
    private bool isSmallScreen;
    private int responseCounter;
    private Channel<string>? channel;
    private AiChatMessage? lastAssistantMessage;
    private List<AiChatMessage> chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.
    //#if(module == "Sales")
    private Action unsubSearchProducts = default!;
    //#endif
    //#if(ads == true)
    private Action unsubAdHaveTrouble = default!;
    //#endif


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    public AppThemeType? CurrentTheme { get; set; }

    [CascadingParameter(Name = Parameters.CurrentDir)]
    public BitDir? CurrentDir { get; set; }


    protected override Task OnInitAsync()
    {
        //#if(module == "Sales")
        unsubSearchProducts = PubSubService.Subscribe(ClientPubSubMessages.SEARCH_PRODUCTS, async (value) =>
        {
            if (isOpen) return;

            isOpen = true;

            StateHasChanged();

            if (chatMessages.Count > 1) return;

            var message = (string?)value;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = Localizer[nameof(AppStrings.AiChatPanelPrompt3)];
            }

            await SendPromptMessage(message);
        });
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble = PubSubService.Subscribe(ClientPubSubMessages.AD_HAVE_TROUBLE, async _ =>
        {
            if (isOpen) return;

            isOpen = true;

            StateHasChanged();

            var message = Localizer[nameof(AppStrings.UpgradeAdHaveTroublePrompt)];

            await SendPromptMessage(message);
        });
        //#endif

        return base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        SetDefaultValues();
        StateHasChanged();
        hubConnection.Reconnected += HubConnection_Reconnected;

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HubConnection_Reconnected(string? _)
    {
        await RestartChannel();
    }

    private async Task SendPromptMessage(string message)
    {
        userInput = message;
        await SendMessage();
    }

    private async Task SendMessage()
    {
        if (channel is null)
        {
            _ = StartChannel();
        }

        isLoading = true;

        var input = userInput;
        userInput = string.Empty;

        chatMessages.Add(new() { Content = input, Role = AiChatMessageRole.User });
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        chatMessages.Add(lastAssistantMessage);

        StateHasChanged();

        await channel!.Writer.WriteAsync(input!, CurrentCancellationToken);
    }

    private async Task ClearChat()
    {
        SetDefaultValues();

        await RestartChannel();
    }

    private void SetDefaultValues()
    {
        isLoading = false;
        responseCounter = 0;
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        chatMessages = [
            new()
            {
                Role = AiChatMessageRole.Assistant,
                Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)],
            }
        ];
    }

    private async Task HandleOnDismissPanel()
    {
        await StopChannel();
    }

    private async Task HandleOnUserInputEnter(KeyboardEventArgs e)
    {
        if (e.ShiftKey) return;

        await SendMessage();
    }

    private async Task StartChannel()
    {
        channel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         new StartChatbotRequest()
                                                                         {
                                                                             CultureId = CultureInfo.CurrentCulture.LCID,
                                                                             DeviceInfo = TelemetryContext.Platform,
                                                                             ChatMessagesHistory = chatMessages
                                                                         },
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            int expectedResponsesCount = chatMessages.Count(c => c.Role is AiChatMessageRole.User);

            if (response is SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS)
            {
                responseCounter++;
                isLoading = false;
            }
            else if (response is SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR)
            {
                responseCounter++;
                if (responseCounter == expectedResponsesCount)
                {
                    isLoading = false; // Hide loading only if this is an error for the last user's message.
                }
                chatMessages[responseCounter * 2].Successful = false;
            }
            else
            {
                if ((responseCounter + 1) == expectedResponsesCount)
                {
                    lastAssistantMessage!.Content += response;
                }
            }

            StateHasChanged();
        }
    }

    private async Task StopChannel()
    {
        if (channel is null) return;

        channel.Writer.Complete();
        channel = null;
    }

    private async Task RestartChannel()
    {
        await StopChannel();
        await StartChannel();
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        //#if(module == "Sales")
        unsubSearchProducts();
        //#endif

        //#if(ads == true)
        unsubAdHaveTrouble();
        //#endif

        hubConnection.Reconnected -= HubConnection_Reconnected;

        await StopChannel();

        await base.DisposeAsync(disposing);
    }
}
