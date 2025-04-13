using System.Threading.Channels;
using Boilerplate.Shared.Dtos.Chatbot;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanel;

public partial class AppAiChatPanel
{
    private bool isOpen;
    private bool isLoading;
    private string? userInput;
    private int responseCounter;
    private Channel<string>? channel;
    private BitTextField textFieldRef = default!;
    private List<AiChatMessage> chatMessages = [];
    private AiChatMessage lastAssistantMessage = new();


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? currentTheme { get; set; }

    [CascadingParameter]
    private BitDir? currentDir { get; set; }


    protected override Task OnInitAsync()
    {
        chatMessages.Add(new() { Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)], Role = AiChatMessageRole.Assistant });

        return base.OnInitAsync();
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
        lastAssistantMessage = new();
        chatMessages.Add(lastAssistantMessage);

        StateHasChanged();

        await channel!.Writer.WriteAsync(input!, CurrentCancellationToken);
    }

    private async Task ClearChat()
    {
        lastAssistantMessage = new();
        chatMessages = [new() { Content = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)], Role = AiChatMessageRole.Assistant }];

        await RestartChannel();
    }

    private async Task HandleOnOpenPanel()
    {
        await textFieldRef.FocusAsync();
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
                                                                         CultureInfo.CurrentCulture.NativeName,
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            if (response is ChatMessageProcessStatus.MESSAGE_RPOCESS_SUCESS)
            {
                responseCounter++;
                isLoading = false;
            }
            else if (response is ChatMessageProcessStatus.MESSAGE_RPOCESS_ERROR)
            {
                if (++responseCounter == (chatMessages.Count - 1) / 2)
                {
                    isLoading = false;
                }

                var index = responseCounter * 2;

                chatMessages[index].Successful = false;
            }
            else
            {
                lastAssistantMessage.Content += response;
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
        await StopChannel();

        await base.DisposeAsync(disposing);
    }
}
