using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil08NotificationPage
{
    private bool? isNotificationSupported;
    private async Task CheckIsSupported() 
    {
        isNotificationSupported = await notification.IsSupported();
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

    private async Task ShowNotification()
    {
        await notification.Show("title", new() { Body = "this is body." });
    }



    private string isSupportedExampleCode =
@"@inject Bit.Butil.Notification notification

<BitText>Notification supported? [@isNotificationSupported]</BitText>

<BitButton OnClick=""CheckIsSupported"">IsSupported</BitButton>

@code {
    private bool? isNotificationSupported;

    private async Task CheckIsSupported()
    {
        isNotificationSupported = await notification.IsSupported();
    }
}";
    private string getPermissionExampleCode =
@"@inject Bit.Butil.Notification notification

<BitText>Current permission state: [@permissionResult]</BitText>

<BitButton OnClick=""GetCurrentPermissionState"">GetPermission</BitButton>

@code {
    private NotificationPermission? permissionResult = null;

    private async Task GetCurrentPermissionState()
    {
        permissionResult = await notification.GetPermission();
    }
}";
    private string requestPermissionExampleCode =
@"@inject Bit.Butil.Notification notification

<BitText>Request permission result: [@requestPermissionResult]</BitText>

<BitButton OnClick=""RequestPermission"">RequestPermission</BitButton>

@code {
    private NotificationPermission? requestPermissionResult = null;

    private async Task RequestPermission()
    {
        requestPermissionResult = await notification.RequestPermission();
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
}
