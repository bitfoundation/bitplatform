namespace Boilerplate.Client.Core.Services;

public partial class SnackBarService
{
    [AutoInject] private readonly PubSubService pubSubService = default!;


    public void Show(string title, string body = "", BitColor color = BitColor.Info)
    {
        pubSubService.Publish(ClientPubSubMessages.SHOW_SNACK, (title, body, color), persistent: true);
    }

    public void Error(string title, string body = "") => Show(title, body, BitColor.Error);
    public void Success(string title, string body = "") => Show(title, body, BitColor.Success);
}
