namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.SnackBar;

public partial class BitSnackBarDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoDismiss",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether or not to dismiss itself automatically.",
        },
        new()
        {
            Name = "AutoDismissTime",
            Type = "TimeSpan",
            DefaultValue = "TimeSpan.FromSeconds(3)",
            Description = "How long the SnackBar will automatically dismiss (default is 3 seconds).",
        },
        new()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the Body is rendered.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitSnackBarClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#snackbar-class-styles",
            Description = "Custom CSS classes for different parts of the BitSnackBar.",
        },
        new()
        {
            Name = "DismissIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Dismiss Icon in SnackBar.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Callback for when the Dismissed.",
        },
        new()
        {
            Name = "Position",
            Type = "BitSnackBarPosition",
            DefaultValue = "BitSnackBarPosition.BottomRight",
            Description = "The position of SnackBar to show.",
            LinkType = LinkType.Link,
            Href = "#snackbar-position-enum"
        },
        new()
        {
            Name = "Styles",
            Type = "BitSnackBarClassStyles?",
            DefaultValue = "null",
            LinkType = LinkType.Link,
            Href = "#snackbar-class-styles",
            Description = "Custom CSS styles for different parts of the BitSnackBar.",
        },
        new()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment<string>?",
            DefaultValue = "null",
            Description = "Used to customize how content inside the Title is rendered. ",
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
                    Name = "TopLeft",
                    Value = "0",
                },
                new()
                {
                    Name = "TopCenter",
                    Value = "1",
                },
                new()
                {
                    Name = "TopRight",
                    Value = "2",
                },
                new()
                {
                    Name = "BottomLeft",
                    Value = "3",
                },
                new()
                {
                    Name = "BottomCenter",
                    Value = "4",
                },
                new()
                {
                    Name = "BottomRight",
                    Value = "5",
                },
            ]
        },
        new()
        {
            Id = "snackbar-type-enum",
            Name = "BitSnackBarType",
            Items =
            [
                new()
                {
                    Name = "None",
                    Description = "None styled SnackBar",
                    Value = "0",
                },
                new()
                {
                    Name = "Info",
                    Description = "Info styled SnackBar",
                    Value = "1",
                },
                new()
                {
                    Name = "Warning",
                    Description = "Warning styled SnackBar",
                    Value = "2",
                },
                new()
                {
                    Name = "Success",
                    Description = "Success styled SnackBar",
                    Value = "3",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error styled SnackBar",
                    Value = "4",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "Severe Warning styled SnackBar",
                    Value = "5",
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
            Type = "async Task Show(string title, string? body = \"\", BitSnackBarType type = BitSnackBarType.None, string? cssClass = null, string? cssStyle = null)",
            DefaultValue = "",
            Description = "Shows the snackbar.",
        },
        new()
        {
            Name = "Info",
            Type = "Task Info(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Info type.",
        },
        new()
        {
            Name = "Success",
            Type = "Task Success(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Success type.",
        },
        new()
        {
            Name = "Warning",
            Type = "Task Warning(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Warning type.",
        },
        new()
        {
            Name = "Error",
            Type = "Task Error(string title, string? body = \"\")",
            DefaultValue = "",
            Description = "Shows the snackbar with Error type.",
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
    private int basicSnackBarDismissSeconds = 3;
    private bool basicSnackBarAutoDismiss = true;
    private BitSnackBar customizationRef = default!;
    private string basicSnackBarBody = "This is body";
    private string basicSnackBarTitle = "This is title";
    private BitSnackBarType basicSnackBarType = BitSnackBarType.Info;
    private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomRight;

    private async Task OpenCustomizationSnackBar()
    {
        await customizationRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarType);
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

<BitChoiceGroup @bind-Value=""basicSnackBarType"" Label=""Type"" TItem=""BitChoiceGroupOption<BitSnackBarType>"" TValue=""BitSnackBarType"">
    <BitChoiceGroupOption Text=""Info"" Value=""BitSnackBarType.Info"" />
    <BitChoiceGroupOption Text=""Success"" Value=""BitSnackBarType.Success"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitSnackBarType.Warning"" />
    <BitChoiceGroupOption Text=""SevereWarning"" Value=""BitSnackBarType.SevereWarning"" />
    <BitChoiceGroupOption Text=""Error"" Value=""BitSnackBarType.Error"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""basicSnackBarPosition"" Label=""Position"" TItem=""BitChoiceGroupOption<BitSnackBarPosition>"" TValue=""BitSnackBarPosition"">
    <BitChoiceGroupOption Text=""TopLeft"" Value=""BitSnackBarPosition.TopLeft"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackBarPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopRight"" Value=""BitSnackBarPosition.TopRight"" />
    <BitChoiceGroupOption Text=""BottomLeft"" Value=""BitSnackBarPosition.BottomLeft"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackBarPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomRight"" Value=""BitSnackBarPosition.BottomRight"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""direction"" Label=""Direction"" TItem=""BitChoiceGroupOption<BitDir>"" TValue=""BitDir"">
    <BitChoiceGroupOption Text=""LTR"" Value=""BitDir.Ltr"" />
    <BitChoiceGroupOption Text=""RTL"" Value=""BitDir.Rtl"" />
    <BitChoiceGroupOption Text=""Auto"" Value=""BitDir.Auto"" />
</BitChoiceGroup>

<BitTextField @bind-Value=""basicSnackBarTitle"" Label=""Title"" DefaultValue=""Title"" />
<BitTextField @bind-Value=""basicSnackBarBody"" Label=""Body"" IsMultiline=""true"" Rows=""6"" DefaultValue=""This is a body!"" />

<BitToggle @bind-Value=""basicSnackBarAutoDismiss"" Label=""Auto Dismiss"" />
<BitNumberField @bind-Value=""basicSnackBarDismissSeconds"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />

<BitButton OnClick=""OpenBasicSnackBar"">Show</BitButton>";
    private readonly string example2CsharpCode = @"
private BitDir direction;
private int basicSnackBarDismissSeconds = 3;
private bool basicSnackBarAutoDismiss = true;
private BitSnackBar customizationRef = default!;
private string basicSnackBarBody = ""This is body"";
private string basicSnackBarTitle = ""This is title"";
private BitSnackBarType basicSnackBarType = BitSnackBarType.Info;
private BitSnackBarPosition basicSnackBarPosition = BitSnackBarPosition.BottomRight;

private async Task OpenBasicSnackBar()
{
    await basicSnackBarRef.Show(basicSnackBarTitle, basicSnackBarBody, basicSnackBarType);
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
        background-color: gold;
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

<BitSnackBar @ref=""snackBarClassesRef"" 
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
