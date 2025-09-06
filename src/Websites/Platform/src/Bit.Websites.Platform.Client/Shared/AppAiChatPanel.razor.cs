using System.Threading.Channels;
using Bit.Websites.Platform.Shared.Dtos.AiChat;
using Bit.Websites.Platform.Shared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.Websites.Platform.Client.Shared;

public partial class AppAiChatPanel
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [AutoInject] private HubConnection hubConnection = default!;


    private bool isOpen;
    private bool isLoading;
    private string? userInput;
    private bool isSmallScreen;
    private int responseCounter;
    private Channel<string>? channel;
    private AiChatMessage? lastAssistantMessage;
    private List<AiChatMessage> chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.

    private string AiChatPanelPrompt1 = "What does bitplatform MIT license mean? Is it free to use?";
    private string AiChatPanelPrompt2 = "What are the benefits of dedicated support?";
    private string AiChatPanelPrompt3 = "What does bit Besql do?";


    protected override async Task OnAfterFirstRenderAsync()
    {
        SetDefaultValues();
        StateHasChanged();
        hubConnection.Reconnected += HubConnection_Reconnected;

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HubConnection_Reconnected(string? _)
    {
        if (channel is null) return;

        await RestartChannel();
    }

    private async Task SendPromptMessage(string message)
    {
        userInput = message;
        await SendMessage();
    }

    private async Task SendMessage()
    {
        if (hubConnection.State is not HubConnectionState.Connected)
        {
            await hubConnection.StartAsync(CurrentCancellationToken);
        }

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
                Content = "I'm here to make your app experience awesome! Got a question or need a hand?",
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
        hubConnection.Reconnected -= HubConnection_Reconnected;

        await StopChannel();

        await base.DisposeAsync(disposing);
    }
}
