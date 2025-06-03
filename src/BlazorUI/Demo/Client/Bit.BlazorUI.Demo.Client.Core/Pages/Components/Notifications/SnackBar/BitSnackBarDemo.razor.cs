namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.SnackBar;

public partial class BitSnackBarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoDismiss",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not automatically dismiss the snack bar.",
        },
        new()
        {
            Name = "AutoDismissTime",
            Type = "TimeSpan?",
            DefaultValue = "null",
            Description = "How long does it take to automatically dismiss the snack bar (default is 3 seconds).",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how the content inside the body is rendered.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSnackBarClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the snack bar.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "The icon name of the dismiss button.",
        },
        new()
        {
            Name = "Multiline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the multiline mode of both title and body.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Callback for when any snack bar is dismissed.",
        },
        new()
        {
            Name = "Persistent",
            Type = "bool",
            DefaultValue = "false",
            Description = "Makes the snack bar non-dismissible in UI and removes the dismiss button.",
        },
        new()
        {
            Name = "Position",
            Type = "BitSnackBarPosition?",
            DefaultValue = "null",
            Description = "The position of the snack bars to show.",
            LinkType = LinkType.Link,
            Href = "#snackbar-position-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitSnackBarClassStyles?",
            Description = "Custom CSS styles for different parts of the snack bar.",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the title is rendered. ",
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "snackbar-position-enum",
            Name = "BitSnackBarPosition",
            Items =
            [
                new()
                {
                    Name = "TopStart",
                    Value = "0",
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1",
                },
                new()
                {
                    Name = "TopEnd",
                    Value = "2",
                },
                new()
                {
                    Name = "BottomStart",
                    Value = "3",
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "4",
                },
                new()
                {
                    Name = "BottomEnd",
                    Value = "5",
                },
            ]
        },
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name = "Primary",
                    Description = "Info Primary general color.",
                    Value = "0",
                },
                new()
                {
                    Name = "Secondary",
                    Description = "Secondary general color.",
                    Value = "1",
                },
                new()
                {
                    Name = "Tertiary",
                    Description = "Tertiary general color.",
                    Value = "2",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info general color.",
                    Value = "3",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success general color.",
                    Value = "4",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning general color.",
                    Value = "5",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "SevereWarning general color.",
                    Value = "6",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error general color.",
                    Value = "7",
                },
                new()
                {
                    Name = "PrimaryBackground",
                    Description = "Primary background color.",
                    Value = "8",
                },
                new()
                {
                    Name = "SecondaryBackground",
                    Description = "Secondary background color.",
                    Value = "9",
                },
                new()
                {
                    Name = "TertiaryBackground",
                    Description = "Tertiary background color.",
                    Value = "10",
                },
                new()
                {
                    Name = "PrimaryForeground",
                    Description = "Primary foreground color.",
                    Value = "11",
                },
                new()
                {
                    Name = "SecondaryForeground",
                    Description = "Secondary foreground color.",
                    Value = "12",
                },
                new()
                {
                    Name = "TertiaryForeground",
                    Description = "Tertiary foreground color.",
                    Value = "13",
                },
                new()
                {
                    Name = "PrimaryBorder",
                    Description = "Primary border color.",
                    Value = "14",
                },
                new()
                {
                    Name = "SecondaryBorder",
                    Description = "Secondary border color.",
                    Value = "15",
                },
                new()
                {
                    Name = "TertiaryBorder",
                    Description = "Tertiary border color.",
                    Value = "16",
                }
            ]
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitSnackBarClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitSnackBar."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the main container of the BitSnackBar."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitSnackBar."
                },
                new()
                {
                    Name = "DismissButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss button of the BitSnackBar."
                },
                new()
                {
                    Name = "DismissIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the dismiss icon of the BitSnackBar."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitSnackBar."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the BitSnackBar."
                },
                new()
                {
                    Name = "ProgressBar",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress bar of the BitSnackBar."
                }
            ]
        },
        new()
        {
            Id = "snackbar-item",
            Title = "BitSnackBarItem",
            Description = "A class to represent each snack bar item.",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "Guid",
                    DefaultValue = "Guid.NewGuid()",
                    Description = "The unique identifier of the snack bar item."
                },
                new()
                {
                    Name = "Title",
                    Type = "string",
                    DefaultValue = "null",
                    Description = "The title text of the snack bar item."
                },
                new()
                {
                    Name = "Body",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The body text of the snack bar item."
                },
                new()
                {
                    Name = "Color",
                    Type = "BitColor?",
                    DefaultValue = "null",
                    Description = "The color theme of the snack bar item.",
                    LinkType = LinkType.Link,
                    Href = "#color-enum",
                },
                new()
                {
                    Name = "CssClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS class to apply to the snack bar item."
                },
                new()
                {
                    Name = "CssStyle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS style to apply to the snack bar item."
                },
                new()
                {
                    Name = "Persistent",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Makes this specific snack bar item non-dismissible and removes its dismiss button."
                },
            ]
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "Info",
            Type = "Task<BitSnackBarItem> Info(string title, string? body = \"\", bool persistent = false)",
            Description = "Shows a new snackbar with Info color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Success",
            Type = "Task<BitSnackBarItem> Success(string title, string? body = \"\", bool persistent = false)",
            Description = "Shows a new snackbar with Success color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Warning",
            Type = "Task<BitSnackBarItem> Warning(string title, string? body = \"\", bool persistent = false)",
            Description = "Shows a new snackbar with Warning color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "SevereWarning",
            Type = "Task<BitSnackBarItem> SevereWarning(string title, string? body = \"\", bool persistent = false)",
            Description = "Shows a new snackbar with SevereWarning color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Error",
            Type = "Task<BitSnackBarItem> Error(string title, string? body = \"\", bool persistent = false)",
            Description = "Shows a new snackbar with Error color.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitSnackBarItem> Show(string title, string? body = \"\", BitColor color = BitColor.Info, string? cssClass = null, string? cssStyle = null, bool persistent = false)",
            Description = "Shows a new snackbar.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Show",
            Type = "Task<BitSnackBarItem> Show(BitSnackBarItem item)",
            Description = "Shows a new snackbar.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
        new()
        {
            Name = "Close",
            Type = "Task Close(BitSnackBarItem item)",
            Description = "Closes a snackbar item.",
            LinkType = LinkType.Link,
            Href = "#snackbar-item",
        },
    ];



    private BitSnackBar basicRef = default!;
    private async Task OpenBasicSnackBar()
    {
        await basicRef.Info("This is title", "This is body");
    }


    private BitSnackBarItem? persistentItem;
    private BitSnackBar persistentRef = default!;
    private async Task OpenPersistentSnackBar()
    {
        await ClosePersistentSnackBar();

        persistentItem = await persistentRef.Info("This is persistent title", "This is persistent body");
    }
    private async Task ClosePersistentSnackBar()
    {
        if (persistentItem is not null)
        {
            await persistentRef.Close(persistentItem);
            persistentItem = null;
        }
    }


    private string? bodyTemplateAnswer;
    private BitSnackBar bodyTemplateRef = default!;
    private BitSnackBar titleTemplateRef = default!;
    private BitSnackBar dismissIconNameRef = default!;

    private async Task OpenDismissIconName()
    {
        await dismissIconNameRef.Success("This is title", "This is body");
    }

    private async Task OpenTitleTemplate()
    {
        await titleTemplateRef.Warning("This is title", "This is body");
    }

    private async Task OpenBodyTemplate()
    {
        await bodyTemplateRef.Error("This is title", "This is body");
    }


    private BitDir direction;
    private bool basicSnackBarMultiline;
    private bool basicSnackBarAutoDismiss;
    private int basicSnackBarDismissSeconds = 3;
    private BitSnackBar customizationRef = default!;
    private string basicSnackBarBody = "This is body";
    private string basicSnackBarTitle = "This is title";
    private BitColor basicSnackBarColor = BitColor.Info;
    private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomEnd;

    private async Task OpenCustomizationSnackBar()
    {
        await customizationRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarColor);
    }


    private BitSnackBar snackBarStyleRef = default!;
    private BitSnackBar snackBarClassRef = default!;
    private BitSnackBar snackBarStylesRef = default!;
    private BitSnackBar snackBarClassesRef = default!;

    private async Task OpenSnackBarStyle()
    {
        await snackBarClassRef.Show("This is title", "This is body", cssStyle: "background-color: dodgerblue; border-radius: 0.5rem;");
    }

    private async Task OpenSnackBarClass()
    {
        await snackBarStyleRef.Show("This is title", "This is body", cssClass: "custom-class");
    }

    private async Task OpenSnackBarStyles()
    {
        await snackBarStylesRef.Show("This is title", "This is body");
    }

    private async Task OpenSnackBarClasses()
    {
        await snackBarClassesRef.Show("This is title", "This is body");
    }
}
