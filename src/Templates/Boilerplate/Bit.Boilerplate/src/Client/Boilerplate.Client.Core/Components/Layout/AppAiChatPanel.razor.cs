using System.Threading.Channels;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppAiChatPanel
{
    private static string initialResponse = "This is the AI initial response to kick start the chat!";

    private bool isOpen;
    private string? userInput;
    private Channel<string>? channel;
    private bool isCommunicating = false;
    private BitTextField textFieldRef = default!;
    private string lastAssistantResponse = string.Empty;
    private List<string> conversation = [initialResponse];


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? currentTheme { get; set; }

    [CascadingParameter]
    private BitDir? currentDir { get; set; }


    private async Task SendMessage()
    {
        if (channel is null)
        {
            _ = StartChannel();
        }

        if (isCommunicating)
        {
            // stopping the channel?
            return;
        }

        if (string.IsNullOrWhiteSpace(userInput)) return;

        isCommunicating = true;

        var input = userInput;
        userInput = string.Empty;

        conversation.Add(input);
        lastAssistantResponse = string.Empty;

        StateHasChanged();

        await channel!.Writer.WriteAsync(input, CurrentCancellationToken);
    }

    private async Task OpenPanel()
    {
        isOpen = true;

        await Task.Delay(100);

        await textFieldRef.FocusAsync();
    }

    private async Task ClearChat()
    {
        conversation = [initialResponse];
        lastAssistantResponse = string.Empty;

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
        channel = Channel.CreateUnbounded<string>(new() { SingleWriter = true, SingleReader = true });

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         CultureInfo.CurrentCulture.NativeName,
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            if (response is "MESSAGE_PROCESSED")
            {
                isCommunicating = false;
                conversation.Add(lastAssistantResponse);
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
