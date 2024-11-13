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
            Href = "#snackbar-class-styles",
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
            Href = "#snackbar-class-styles",
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
            Id = "snackbar-class-styles",
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
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers = 
    [
        new()
        {
            Name = "Show",
            Type = "async Task Show(string title, string? body = \"\", BitColor color = BitColor.Info, string? cssClass = null, string? cssStyle = null)",
            DefaultValue = "",
            Description = "Shows the snackbar.",
        },
        new()
        {
            Name = "Info",
            Type = "Task Info(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Info color.",
        },
        new()
        {
            Name = "Success",
            Type = "Task Success(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Success color.",
        },
        new()
        {
            Name = "Warning",
            Type = "Task Warning(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Warning color.",
        },
        new()
        {
            Name = "SevereWarning",
            Type = "Task Warning(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with SevereWarning color.",
        },
        new()
        {
            Name = "Error",
            Type = "Task Error(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Error color.",
        }
    ];



    private BitSnackBar basicRef = default!;
    private async Task OpenBasicSnackBar()
    {
        await basicRef.Info("This is title", "This is body");
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



    private readonly string example1RazorCode = @"
<BitSnackBar @ref=""basicRef"" />
<BitButton OnClick=""OpenBasicSnackBar"">Open SnackBar</BitButton>";
    private readonly string example1CsharpCode = @"
private BitSnackBar basicRef = default!;
private async Task OpenBasicSnackBar()
{
    await basicRef.Info(""This is title"", ""This is body"");
}";

    private readonly string example2RazorCode = @"
<BitSnackBar @ref=""basicSnackBarRef""
             Dir=""direction""
             Position=""@basicSnackBarPosition""
             AutoDismiss=""@basicSnackBarAutoDismiss""
             AutoDismissTime=""TimeSpan.FromSeconds(basicSnackBarDismissSeconds)"" />

<BitButton OnClick=""OpenBasicSnackBar"">Show</BitButton>

<BitChoiceGroup @bind-Value=""basicSnackBarColor"" Label=""Type"" TItem=""BitChoiceGroupOption<BitColor>"" TValue=""BitColor"">
    <BitChoiceGroupOption Text=""Primary"" Value=""BitColor.Primary"" />
    <BitChoiceGroupOption Text=""Secondary"" Value=""BitColor.Secondary"" />
    <BitChoiceGroupOption Text=""Tertiary"" Value=""BitColor.Tertiary"" />
    <BitChoiceGroupOption Text=""Info"" Value=""BitColor.Info"" />
    <BitChoiceGroupOption Text=""Success"" Value=""BitColor.Success"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitColor.Warning"" />
    <BitChoiceGroupOption Text=""SevereWarning"" Value=""BitColor.SevereWarning"" />
    <BitChoiceGroupOption Text=""Error"" Value=""BitColor.Error"" />
    <BitChoiceGroupOption Text=""PrimaryBackground"" Value=""BitColor.PrimaryBackground"" />
    <BitChoiceGroupOption Text=""SecondaryBackground"" Value=""BitColor.SecondaryBackground"" />
    <BitChoiceGroupOption Text=""TertiaryBackground"" Value=""BitColor.TertiaryBackground"" />
    <BitChoiceGroupOption Text=""PrimaryForeground"" Value=""BitColor.PrimaryForeground"" />
    <BitChoiceGroupOption Text=""SecondaryForeground"" Value=""BitColor.SecondaryForeground"" />
    <BitChoiceGroupOption Text=""TertiaryForeground"" Value=""BitColor.TertiaryForeground"" />
    <BitChoiceGroupOption Text=""PrimaryBorder"" Value=""BitColor.PrimaryBorder"" />
    <BitChoiceGroupOption Text=""SecondaryBorder"" Value=""BitColor.SecondaryBorder"" />
    <BitChoiceGroupOption Text=""TertiaryBorder"" Value=""BitColor.TertiaryBorder"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""basicSnackBarPosition"" Label=""Position"" TItem=""BitChoiceGroupOption<BitSnackBarPosition>"" TValue=""BitSnackBarPosition"">
    <BitChoiceGroupOption Text=""TopStart"" Value=""BitSnackBarPosition.TopStart"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackBarPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopEnd"" Value=""BitSnackBarPosition.TopEnd"" />
    <BitChoiceGroupOption Text=""BottomStart"" Value=""BitSnackBarPosition.BottomStart"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackBarPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomEnd"" Value=""BitSnackBarPosition.BottomEnd"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""direction"" Label=""Direction"" TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitToggle @bind-Value=""basicSnackBarAutoDismiss"" Label=""Auto Dismiss"" />
<BitNumberField @bind-Value=""basicSnackBarDismissSeconds"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />

<BitToggle @bind-Value=""basicSnackBarMultiline"" Label=""Multiline"" Inline />

<BitTextField @bind-Value=""basicSnackBarTitle"" Label=""Title"" DefaultValue=""Title"" />
<BitTextField @bind-Value=""basicSnackBarBody"" Label=""Body"" Multiline Rows=""6"" DefaultValue=""This is a body!"" />";
    private readonly string example2CsharpCode = @"
private BitDir direction;
private bool basicSnackBarMultiline;
private bool basicSnackBarAutoDismiss;
private int basicSnackBarDismissSeconds = 3;
private BitSnackBar customizationRef = default!;
private string basicSnackBarBody = ""This is body"";
private string basicSnackBarTitle = ""This is title"";
private BitColor basicSnackBarColor = BitColor.Info;
private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomEnd;

private async Task OpenBasicSnackBar()
{
    await basicSnackBarRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarColor);
}";

    private readonly string example3RazorCode = @"
<BitSnackBar @ref=""dismissIconNameRef"" DismissIconName=""@BitIconName.Go"" />
<BitButton OnClick=""OpenDismissIconName"">Dismiss Icon Name</BitButton>

<BitSnackBar @ref=""titleTemplateRef"" AutoDismiss=""false"">
    <TitleTemplate Context=""title"">
        <div style=""display: flex; flex-direction: row; gap: 10px;"">
            <span>@title</span>
            <BitProgress Thickness=""20"" Style=""width: 40px;"" Indeterminate />
        </div>
    </TitleTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenTitleTemplate"">Title Template</BitButton>

<BitSnackBar @ref=""bodyTemplateRef"" AutoDismiss=""false"">
    <BodyTemplate Context=""body"">
        <div style=""display: flex; flex-flow: column nowrap; gap: 5px;"">
            <span style=""font-size: 12px; margin-bottom: 5px;"">@body</span>
            <div style=""display: flex; gap: 10px;"">
                <BitButton OnClick=""@(() => bodyTemplateAnswer = ""Yes"")"">Yes</BitButton>
                <BitButton OnClick=""@(() => bodyTemplateAnswer = ""No"")"">No</BitButton>
            </div>
            <span>Answer: @bodyTemplateAnswer</span>
        </div>
    </BodyTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenBodyTemplate"">Body Template</BitButton>";
    private readonly string example3CsharpCode = @"
private string? bodyTemplateAnswer;
private BitSnackBar bodyTemplateRef = default!;
private BitSnackBar titleTemplateRef = default!;
private BitSnackBar dismissIconNameRef = default!;

private async Task OpenDismissIconName()
{
    await dismissIconNameRef.Success(""This is title"", ""This is body"");
}

private async Task OpenTitleTemplate()
{
    await titleTemplateRef.Warning(""This is title"", ""This is body"");
}

private async Task OpenBodyTemplate()
{
    await bodyTemplateRef.Error(""This is title"", ""This is body"");
}";

    private readonly string example4RazorCode = @"
<style>
    .custom-class {
        background-color: tomato;
        box-shadow: gold 0 0 1rem;
    }

    .custom-container {
        border: 1px solid gold;
    }

    .custom-progress {
        background-color: red;
    }
</style>


<BitSnackBar @ref=""snackBarStyleRef"" />
<BitButton OnClick=""OpenSnackBarStyle"">Custom style</BitButton>

<BitSnackBar @ref=""snackBarClassRef"" />
<BitButton OnClick=""OpenSnackBarClass"">Custom style</BitButton>

<BitSnackBar @ref=""snackBarStylesRef""
             Styles=""@(new() { Container = ""width: 16rem; background-color: purple;"",
                               Header = ""background-color: rebeccapurple; padding: 0.2rem;"" })"" />
<BitButton OnClick=""OpenSnackBarStyles"">Custom styles</BitButton>

<BitSnackBar @ref=""snackBarClassesRef"" AutoDismiss
             Classes=""@(new() { Container = ""custom-container"",
                                ProgressBar = ""custom-progress"" })"" />
<BitButton OnClick=""OpenSnackBarClasses"">Custom classes</BitButton>";
    private readonly string example4CsharpCode = @"
private BitSnackBar snackBarStyleRef = default!;
private BitSnackBar snackBarClassRef = default!;
private BitSnackBar snackBarStylesRef = default!;
private BitSnackBar snackBarClassesRef = default!;

private async Task OpenSnackBarStyle()
{
    await snackBarClassRef.Show(""This is title"", ""This is body"", cssStyle: ""background-color: dodgerblue; border-radius: 0.5rem;"");
}

private async Task OpenSnackBarClass()
{
    await snackBarStyleRef.Show(""This is title"", ""This is body"", cssClass: ""custom-class"");
}

private async Task OpenSnackBarStyles()
{
    await snackBarStylesRef.Show(""This is title"", ""This is body"");
}

private async Task OpenSnackBarClasses()
{
    await snackBarClassesRef.Show(""This is title"", ""This is body"");
}";
}
