using System.Threading.Channels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Layout.AppAiChatPanel;

public partial class AppAiChatPanel
{
    private bool isOpen;
    private string? userInput;
    private Channel<string>? channel;
    private bool isCommunicating = false;
    private BitTextField textFieldRef = default!;
    private List<AiChatMessage> conversation = [];
    private string lastAssistantResponse = string.Empty;


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? currentTheme { get; set; }

    [CascadingParameter]
    private BitDir? currentDir { get; set; }


    protected override Task OnInitAsync()
    {
        conversation.Add(new() { Text = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)], Author = AiChatMessageAuthor.Assistant });

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

        isCommunicating = true;

        var input = userInput;
        userInput = string.Empty;

        conversation.Add(new() { Text = input, Author = AiChatMessageAuthor.User });
        lastAssistantResponse = string.Empty;

        StateHasChanged();

        await channel!.Writer.WriteAsync(input!, CurrentCancellationToken);
    }

    private async Task OpenPanel()
    {
        isOpen = true;

        await Task.Delay(100);

        await textFieldRef.FocusAsync();
    }

    private async Task ClearChat()
    {
        lastAssistantResponse = string.Empty;
        conversation = [new() { Text = Localizer[nameof(AppStrings.AiChatPanelInitialResponse)], Author = AiChatMessageAuthor.Assistant }];

        await StopChannel();
        await StartChannel();
    }

    private async Task HandleOnDismissPanel()
    {
        await StopChannel();
    }

    private async Task HandleOnUserInputKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && e.ShiftKey is false)
        {
            await Task.Delay(10);
            await SendMessage();
        }
    }

    private async Task StartChannel()
    {
        channel = Channel.CreateUnbounded<string>();

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         CultureInfo.CurrentCulture.NativeName,
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            if (response is "MESSAGE_PROCESSED")
            {
                isCommunicating = false;
                conversation.Add(new() { Text = lastAssistantResponse, Author = AiChatMessageAuthor.Assistant });
            }
            else
            {
                lastAssistantResponse += response;
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


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        await StopChannel();

        await base.DisposeAsync(disposing);
    }
}
