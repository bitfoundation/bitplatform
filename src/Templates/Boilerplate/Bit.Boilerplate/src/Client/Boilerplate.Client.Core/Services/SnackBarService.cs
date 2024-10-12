﻿namespace Boilerplate.Client.Core.Services;

public partial class SnackBarService
{
    [AutoInject] private readonly IPubSubService pubSubService = default!;


    public void Show(string title, string body = "", BitColor color = BitColor.Info)
    {
        pubSubService.Publish(PubSubMessages.SHOW_SNACK, (title, body, color));
    }

    public void Error(string title, string body = "") => Show(title, body, BitColor.Error);
    public void Success(string title, string body = "") => Show(title, body, BitColor.Success);
}
