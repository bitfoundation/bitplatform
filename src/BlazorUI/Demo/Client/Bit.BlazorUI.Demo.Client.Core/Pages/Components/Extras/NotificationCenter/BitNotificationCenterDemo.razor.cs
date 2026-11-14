namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NotificationCenter;

public partial class BitNotificationCenterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ClearAllText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the button that forgets every notification of the history (BitSnackBarService.ClearHistory). Defaults to \"Clear all\".",
        },
        new()
        {
            Name = "Classes",
            Type = "BitNotificationCenterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the notification center.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "EmptyTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content the panel shows when there are no notifications, which takes the place of EmptyText.",
        },
        new()
        {
            Name = "EmptyText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text the panel shows when there are no notifications. Defaults to BitStrings.NoItemsFound (\"No items found\").",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text at the top of the panel. Defaults to \"Notifications\".",
        },
        new()
        {
            Name = "HistoryLimit",
            Type = "int",
            DefaultValue = "50",
            Description = "How many notifications the center makes BitSnackBarService keep: the service's HistoryLimit is raised to it when it keeps fewer, and never lowered.",
        },
        new()
        {
            Name = "Icon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "The icon of the bell button. Takes precedence over IconName.",
        },
        new()
        {
            Name = "IconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The name of the icon of the bell button, from the bit BlazorUI icons. Defaults to \"Ringer\".",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the panel is open. Opening it marks every notification it lists as read. (two-way bound)",
        },
        new()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitSnackBarItem>?",
            DefaultValue = "null",
            Description = "The custom template of each notification of the list.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitNotificationCenterClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the notification center.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "TimeZone",
            Type = "TimeZoneInfo?",
            DefaultValue = "null",
            Description = "The time zone the time of each notification is shown in. Defaults to the local time zone - which, in a server-side rendered app, is the server's rather than the user's.",
        },
        new()
        {
            Name = "UnreadAriaLabelFormat",
            Type = "string?",
            DefaultValue = "null",
            Description = "The composite format the unread count joins the name of the bell button with ({0} = the count), so a screen reader hears it with the name. Defaults to \"{0} unread\".",
        },
        new()
        {
            Name = "UnreadItemText",
            Type = "string?",
            DefaultValue = "null",
            Description = "What a screen reader hears of a notification that had not been read when the panel opened, which is shown by its look alone. Defaults to \"Unread\".",
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "UnreadCount",
            Type = "int",
            DefaultValue = "0",
            Description = "How many notifications of the history have not been read yet: the count on the badge of the bell.",
        },
        new()
        {
            Name = "MarkAllAsRead",
            Type = "void MarkAllAsRead()",
            Description = "Marks every notification of the history as read, which clears the badge of the bell.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitNotificationCenterClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitNotificationCenter." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the bell button of the BitNotificationCenter." },
                new() { Name = "Icon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the bell icon of the BitNotificationCenter." },
                new() { Name = "Badge", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the unread count badge on the bell of the BitNotificationCenter." },
                new() { Name = "Callout", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the callout the panel of the BitNotificationCenter opens in." },
                new() { Name = "Panel", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the panel of the BitNotificationCenter." },
                new() { Name = "Header", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header of the panel of the BitNotificationCenter." },
                new() { Name = "HeaderText", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the header text of the panel of the BitNotificationCenter." },
                new() { Name = "ClearAllButton", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the clear all button of the BitNotificationCenter." },
                new() { Name = "List", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the list of notifications of the BitNotificationCenter." },
                new() { Name = "Item", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each notification of the BitNotificationCenter." },
                new() { Name = "UnreadItem", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each notification of the BitNotificationCenter that had not been read when the panel opened." },
                new() { Name = "ItemIcon", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the icon of each notification of the BitNotificationCenter." },
                new() { Name = "ItemTitle", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the title of each notification of the BitNotificationCenter." },
                new() { Name = "ItemBody", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the body of each notification of the BitNotificationCenter." },
                new() { Name = "ItemTime", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the time of each notification of the BitNotificationCenter." },
                new() { Name = "Empty", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the text the panel of the BitNotificationCenter shows when there are no notifications." },
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-NotificationCenter-badge-background", DefaultValue = "var(--bit-clr-err)", Description = "Background of the unread count badge." },
        new() { Name = "--bit-NotificationCenter-badge-color", DefaultValue = "var(--bit-clr-err-text)", Description = "Text color of the unread count badge." },
        new() { Name = "--bit-NotificationCenter-width", DefaultValue = "360px", Description = "Width of the panel, never wider than the viewport." },
        new() { Name = "--bit-NotificationCenter-max-height", DefaultValue = "420px", Description = "Height the list of notifications scrolls past." },
        new() { Name = "--bit-NotificationCenter-new-background", DefaultValue = "var(--bit-clr-bg-sec)", Description = "Background of a notification that had not been read when the panel opened." },
    ];



    [AutoInject] private BitSnackBarService snackBarService { get; set; } = default!;

    private BitNotificationCenter? unreadCenter;
    private bool isCenterOpen;

    private Task ShowInfo() => snackBarService.Info("New comment", "Sara replied to your review.");

    private Task ShowSuccess() => snackBarService.Success("Saved", "All changes are saved.");

    private Task ShowWarning() => snackBarService.Warning("Storage almost full", "You have used 92% of your space.");

    private Task ShowError() => snackBarService.Error("Upload failed", "The server did not answer.");

    private Task ShowRtl() => snackBarService.Info("دیدگاه تازه", "سارا به بازبینی شما پاسخ داد.");
}
