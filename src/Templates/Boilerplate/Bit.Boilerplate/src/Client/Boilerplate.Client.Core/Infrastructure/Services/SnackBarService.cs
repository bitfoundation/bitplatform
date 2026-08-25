namespace Boilerplate.Client.Core.Infrastructure.Services;

public partial class SnackBarService
{
    [AutoInject] private readonly PubSubService pubSubService = default!;


    public void Show(string title, string body = "", BitColor color = BitColor.Info)
    {
        pubSubService.Publish(ClientAppMessages.SHOW_SNACK, (title, body, color), persistent: true);
    }

    public void Info(string title, string body = "") => Show(title, body, BitColor.Info);
    public void Success(string title, string body = "") => Show(title, body, BitColor.Success);
    public void Warning(string title, string body = "") => Show(title, body, BitColor.Warning);
    public void Error(string title, string body = "") => Show(title, body, BitColor.Error);
}
