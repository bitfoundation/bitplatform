using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppAiChatPanel
{
    private bool isOpen;
    private string? userInput;
    private bool isCommunicating = false;
    private BitTextField textFieldRef = default!;
    private string lastAssistantResponse = string.Empty;
    private readonly List<string> conversations = [];
    private readonly Channel<string> channel = Channel.CreateUnbounded<string>(new() { SingleWriter = true, SingleReader = true });


    [AutoInject] private HubConnection hubConnection = default!;


    [CascadingParameter(Name = Parameters.CurrentTheme)]
    private AppThemeType? currentTheme { get; set; }

    [CascadingParameter] 
    private BitDir? currentDir { get; set; }


    protected override async Task OnAfterFirstRenderAsync()
    {
        await StartChat();

        await base.OnAfterFirstRenderAsync();
    }


    private async Task OpenPanel()
    {
        isOpen = true;

        await Task.Delay(100);

        await textFieldRef.FocusAsync();
    }

    private async Task StartChat()
    {
        //#if (captcha == "reCaptcha")
        var googleRecpatchaToken = "";
        //#endif

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         //#if (captcha == "reCaptcha")
                                                                         googleRecpatchaToken,
                                                                         //#endif
                                                                         CultureInfo.CurrentCulture.NativeName,
                                                                         channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            if (response is "MESSAGE_PROCESSED")
            {
                isCommunicating = false;
                conversations.Add(lastAssistantResponse);
            }
            else
            {
                lastAssistantResponse += response;
            }

            StateHasChanged();
        }
    }

    private async Task SendMessage()
    {
        if (isCommunicating)
        {
            // stopping the channel?
            return;
        }

        if (string.IsNullOrWhiteSpace(userInput)) return;

        isCommunicating = true;

        var input = userInput;
        userInput = string.Empty;

        conversations.Add(input);
        lastAssistantResponse = string.Empty;

        StateHasChanged();

        await channel.Writer.WriteAsync(input, CurrentCancellationToken);
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        channel.Writer.Complete();

        await base.DisposeAsync(disposing);
    }
}
