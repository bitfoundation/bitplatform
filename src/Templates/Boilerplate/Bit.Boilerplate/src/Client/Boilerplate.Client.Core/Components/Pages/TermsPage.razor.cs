using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class TermsPage
{
    [AutoInject] private HubConnection hubConnection = default!;

    private string userQuery = "";
    private bool isCommunicating = false;
    private string lastAssistantResponse = "";
    private List<string> assistantResponses = [];
    private Channel<string> channel = Channel.CreateUnbounded<string>(new() { SingleWriter = true, SingleReader = true });

    private async Task SendMessage()
    {
        isCommunicating = true;
        lastAssistantResponse = "";
        await channel.Writer.WriteAsync(userQuery, CurrentCancellationToken);
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        //#if (captcha == "reCaptcha")
        var googleRecpatchaToken = "";
        //#endif

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
             //#if (captcha == "reCaptcha")
             googleRecpatchaToken,
             //#endif
             CultureInfo.CurrentCulture.NativeName,
             channel.Reader.ReadAllAsync(CurrentCancellationToken),
             cancellationToken: CurrentCancellationToken
         ))
        {
            if (response is "MESSAGE_PROCESSED")
            {
                userQuery = "";
                isCommunicating = false;
                assistantResponses.Insert(0, lastAssistantResponse);
            }
            else
            {
                lastAssistantResponse += response;
            }
            StateHasChanged();
        }
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        channel.Writer.Complete();

        await base.DisposeAsync(disposing);
    }
}
