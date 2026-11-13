namespace Boilerplate.Client.Core.Components.Layout.AiChat;

public interface IAppAiChat
{
    Task SendPrompt(string prompt);

    Task Clear();

    Task OnPanelClosed();
}
