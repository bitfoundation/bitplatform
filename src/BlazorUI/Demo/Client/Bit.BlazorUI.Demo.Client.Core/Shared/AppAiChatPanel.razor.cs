using System.Threading.Channels;
using Bit.BlazorUI.Demo.Shared.Dtos.AiChat;
using Bit.BlazorUI.Demo.Shared.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class AppAiChatPanel
{
    [AutoInject] private HubConnection hubConnection = default!;



    private bool _isOpen;
    private bool _isLoading;
    private string? _userInput;
    private bool _isSmallScreen;
    private int _responseCounter;
    private Channel<string>? _channel;
    private BitDebouncer _debouncer = new();
    private AiChatMessage? _lastAssistantMessage;
    private List<AiChatMessage> _chatMessages = []; // TODO: Persist these values in client-side storage to retain them across app restarts.

    private string AiChatPanelPrompt1 = "How can I start developing using bit BlazorUI packages?";
    private string AiChatPanelPrompt2 = "How can I benefit from the theme implementation in bit BlazorUI?";
    private string AiChatPanelPrompt3 = "Explain BitGrid and BitStack for a developer familiar with Bootstrap's grid system?";


    protected override async Task OnAfterFirstRenderAsync()
    {
        SetDefaultValues();
        StateHasChanged();
        hubConnection.Reconnected += HubConnection_Reconnected;

        await base.OnAfterFirstRenderAsync();
    }


    private async Task HubConnection_Reconnected(string? _)
    {
        if (_channel is null) return;

        await RestartChannel();
    }

    private async Task SendPromptMessage(string message)
    {
        _userInput = message;
        await SendMessage();
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_userInput)) return;

        if (hubConnection.State is not HubConnectionState.Connected)
        {
            await hubConnection.StartAsync(CurrentCancellationToken);
        }

        if (_channel is null)
        {
            _ = StartChannel();
        }

        _isLoading = true;

        var input = _userInput;
        _userInput = string.Empty;

        _chatMessages.Add(new() { Content = input, Role = AiChatMessageRole.User });
        _lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        _chatMessages.Add(_lastAssistantMessage);

        StateHasChanged();

        await _channel!.Writer.WriteAsync(input!, CurrentCancellationToken);
    }

    private async Task ClearChat()
    {
        SetDefaultValues();

        await RestartChannel();
    }

    private void SetDefaultValues()
    {
        _isLoading = false;
        _responseCounter = 0;
        _lastAssistantMessage = new() { Role = AiChatMessageRole.Assistant };
        _chatMessages = [
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
        _channel = Channel.CreateUnbounded<string>(new() { SingleReader = true, SingleWriter = true });

        await foreach (var response in hubConnection.StreamAsync<string>("Chatbot",
                                                                         new StartChatbotRequest()
                                                                         {
                                                                             ChatMessagesHistory = _chatMessages
                                                                         },
                                                                         _channel.Reader.ReadAllAsync(CurrentCancellationToken),
                                                                         cancellationToken: CurrentCancellationToken))
        {
            int expectedResponsesCount = _chatMessages.Count(c => c.Role is AiChatMessageRole.User);

            if (response is SharedChatProcessMessages.MESSAGE_RPOCESS_SUCESS)
            {
                _responseCounter++;
                _isLoading = false;
            }
            else if (response is SharedChatProcessMessages.MESSAGE_RPOCESS_ERROR)
            {
                _responseCounter++;
                if (_responseCounter == expectedResponsesCount)
                {
                    _isLoading = false; // Hide loading only if this is an error for the last user's message.
                }
                _chatMessages[_responseCounter * 2].Successful = false;
            }
            else
            {
                if ((_responseCounter + 1) == expectedResponsesCount)
                {
                    _lastAssistantMessage!.Content += response;
                    if (response.HasValue())
                    {
                        _ = _debouncer.Do(100, () => Task.Delay(100).ContinueWith(_ =>
                        {
                            JSRuntime.InvokeVoid("highlightSnippet", "chat-messages-stack");
                        }));
                    }
                }
            }

            StateHasChanged();
        }
    }

    private async Task StopChannel()
    {
        if (_channel is null) return;

        _channel.Writer.Complete();
        _channel = null;
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
