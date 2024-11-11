using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil08NotificationPage
{
    private async Task CheckIsSupported() 
    {
        //var isNotificationSupported = await notification.IsSupported();
    }

    private async Task ShowNotification()
    {
        await notification.Show("title", new() { Body = "this is body." });
    }

    private NotificationPermission? permissionResult = null;

    private async Task GetCurrentPermissionState()
    {
        permissionResult = await notification.GetPermission();
    }

    private NotificationPermission? requestPermissionResult = null;

    private async Task RequestPermission()
    {
        requestPermissionResult = await notification.RequestPermission();
    }



    private string isSupportedExampleCode =
@"@inject Bit.Butil.Notification notification

<BitButton OnClick=""CheckIsSupported"">IsSupported</BitButton>

@code {
    private async Task CheckIsSupported()
    {
        var isNotificationSupported = await notification.IsSupported();
    }
}";
    private string showExampleCode =
@"@inject Bit.Butil.Notification notification

<BitButton OnClick=""ShowNotification"">Show</BitButton>

@code {
    private async Task ShowNotification()
    {
        await notification.Show(""title"", new() { Body = ""this is body."" });
    }
}";
    private string getPermissionExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.CountReset(value))"">CountReset</BitButton>

@code {
    private string value = ""Test"";
}";
    private string requestPermissionExampleCode =
@"@inject Bit.Butil.Console console

<BitTextField @bind-Value=""value"" />

<BitButton OnClick=""@(() => console.Debug(value))"">Debug</BitButton>

@code {
    private string value = ""Test"";
}";
}
