namespace Boilerplate.Client.Core.Services;

public partial class MessageBoxService
{
    [AutoInject] private BitModalService modalService = default!;

    public void Show(string message, string title = "")
    {
        Dictionary<string, object> parameters = new()
        {
            { nameof(MessageBox.Title), title },
            { nameof(MessageBox.Body), message }
        };

        _ = modalService.Show<MessageBox>(parameters);
    }
}
