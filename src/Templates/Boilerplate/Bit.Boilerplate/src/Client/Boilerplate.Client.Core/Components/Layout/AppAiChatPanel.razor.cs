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
    private int responseCounter;
    private Channel<string>? channel;
    private AiChatMessage? lastAssistantMessage;
    private BitTextField textFieldRef = default!;
    private List<AiChatMessage> chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? currentTheme { get; set; }

    [CascadingParameter]
    private BitDir? currentDir { get; set; }


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
        lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        chatMessages = [new() { Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)], Role = AiChatMessageRole.Assistant }];
        responseCounter = 0;
    }

    private async Task HandleOnOpenPanel()
    {
        //await textFieldRef.FocusAsync();
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
        channel = Channel.CreateUnbounded<string>();

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         new StartChatbotRequest()
                                                                         {
                                                                             Culture = CultureInfo.CurrentCulture.Name,
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
                    isLoading = false; // Hide loading only if this is a error for the last user's message.
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
