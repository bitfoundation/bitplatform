namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NotificationCenter;

public partial class BitNotificationCenterDemo
{
    private readonly string example1RazorCode = @"
<BitSnackBar ServiceHost ShowIcon AutoDismiss AutoDismissTime=""TimeSpan.FromSeconds(4)"" />

<BitNotificationCenter />

<BitButton OnClick=""ShowInfo"">Info</BitButton>
<BitButton OnClick=""ShowSuccess"">Success</BitButton>
<BitButton OnClick=""ShowWarning"">Warning</BitButton>
<BitButton OnClick=""ShowError"">Error</BitButton>";
    private readonly string example1CsharpCode = @"
// Program.cs: builder.Services.AddBitBlazorUIServices();

[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private Task ShowInfo() => snackBarService.Info(""New comment"", ""Sara replied to your review."");
private Task ShowSuccess() => snackBarService.Success(""Saved"", ""All changes are saved."");
private Task ShowWarning() => snackBarService.Warning(""Storage almost full"", ""You have used 92% of your space."");
private Task ShowError() => snackBarService.Error(""Upload failed"", ""The server did not answer."");";

    private readonly string example2RazorCode = @"
<BitNotificationCenter @ref=""unreadCenter"" @bind-IsOpen=""isCenterOpen"" />

<BitButton OnClick=""ShowSuccess"">Notify</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isCenterOpen = true"">Open</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => unreadCenter?.MarkAllAsRead()"">Mark all as read</BitButton>
<BitText>Unread: @unreadCenter?.UnreadCount</BitText>";
    private readonly string example2CsharpCode = @"
[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private BitNotificationCenter? unreadCenter;
private bool isCenterOpen;

private Task ShowSuccess() => snackBarService.Success(""Saved"", ""All changes are saved."");";

    private readonly string example3RazorCode = @"
<BitNotificationCenter>
    <ItemTemplate Context=""item"">
        <div style=""display: flex; flex-direction: column; gap: 2px;"">
            <b>@item.Title</b>
            <span>@(item.DismissReason is { } reason ? $""Dismissed ({reason})"" : ""On the screen"")</span>
        </div>
    </ItemTemplate>
</BitNotificationCenter>

<BitButton OnClick=""ShowWarning"">Notify</BitButton>";
    private readonly string example3CsharpCode = @"
[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private Task ShowWarning() => snackBarService.Warning(""Storage almost full"", ""You have used 92% of your space."");";

    private readonly string example4RazorCode = @"
<BitNotificationCenter HeaderText=""Inbox""
                       ClearAllText=""Empty the inbox""
                       EmptyText=""You are all caught up.""
                       AriaLabel=""Inbox""
                       UnreadAriaLabelFormat=""{0} new""
                       UnreadItemText=""New""
                       TimeZone=""TimeZoneInfo.Utc"" />

<BitButton OnClick=""ShowInfo"">Notify</BitButton>";
    private readonly string example4CsharpCode = @"
[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private Task ShowInfo() => snackBarService.Info(""New comment"", ""Sara replied to your review."");";

    private readonly string example5RazorCode = @"
<BitNotificationCenter Style=""--bit-NotificationCenter-badge-background: #6a1b9a; --bit-NotificationCenter-width: 300px;""
                       Styles=""@(new() { HeaderText = ""color: #6a1b9a;"", ItemTitle = ""font-style: italic;"" })"" />

<BitButton OnClick=""ShowError"">Notify</BitButton>";
    private readonly string example5CsharpCode = @"
[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private Task ShowError() => snackBarService.Error(""Upload failed"", ""The server did not answer."");";

    private readonly string example6RazorCode = @"
<div dir=""rtl"">
    <BitNotificationCenter Dir=""BitDir.Rtl""
                           HeaderText=""اعلان‌ها""
                           ClearAllText=""پاک کردن همه""
                           EmptyText=""اعلانی وجود ندارد""
                           AriaLabel=""اعلان‌ها""
                           UnreadAriaLabelFormat=""{0} خوانده نشده""
                           UnreadItemText=""خوانده نشده"" />

    <BitButton OnClick=""ShowRtl"">اعلان</BitButton>
</div>";
    private readonly string example6CsharpCode = @"
[Inject] private BitSnackBarService snackBarService { get; set; } = default!;

private Task ShowRtl() => snackBarService.Info(""دیدگاه تازه"", ""سارا به بازبینی شما پاسخ داد."");";
}
