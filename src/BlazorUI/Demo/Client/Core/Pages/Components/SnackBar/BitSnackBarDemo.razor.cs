namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.SnackBar;

public partial class BitSnackBarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
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
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "snackbar-position-enum",
            Name = "BitSnackBarPosition",
            Items = new()
            {
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
            }
        },
        new()
        {
            Id = "SnackBar-type-enum",
            Name = "BitSnackBarType",
            Items = new()
            {
                new()
                {
                    Name = "Info",
                    Description = "Info styled SnackBar",
                    Value = "0",
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
                    Value = "1",
                },
                new()
                {
                    Name = "Error",
                    Description = "Error styled SnackBar",
                    Value = "3",
                },
                new()
                {
                    Name = "SevereWarning",
                    Description = "Severe Warning styled SnackBar",
                    Value = "2",
                },
            }
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "snackbar-class-styles",
            Title = "BitSnackBarClassStyles",
            Parameters = new()
            {
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
                    Name = "Progress",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the progress of the BitSnackBar."
                }
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitSnackBar @ref=""BasicSnackBarRef""
                Position=""@BasicSnackBarPosition""
                AutoDismiss=""@BasicSnackBarAutoDismiss""
                AutoDismissTime=""TimeSpan.FromSeconds(BasicSnackBarDismissSeconds)"" />

<BitChoiceGroup @bind-Value=""BasicSnackBarType"" Label=""Type"" TItem=""BitChoiceGroupOption<BitSnackBarType>"" TValue=""BitSnackBarType"">
    <BitChoiceGroupOption Text=""Info"" Value=""BitSnackBarType.Info"" />
    <BitChoiceGroupOption Text=""Success"" Value=""BitSnackBarType.Success"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitSnackBarType.Warning"" />
    <BitChoiceGroupOption Text=""Warning"" Value=""BitSnackBarType.SevereWarning"" />
    <BitChoiceGroupOption Text=""Error"" Value=""BitSnackBarType.Error"" />
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""BasicSnackBarPosition"" Label=""Position"" TItem=""BitChoiceGroupOption<BitSnackBarPosition>"" TValue=""BitSnackBarPosition"">
    <BitChoiceGroupOption Text=""TopLeft"" Value=""BitSnackBarPosition.TopLeft"" />
    <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackBarPosition.TopCenter"" />
    <BitChoiceGroupOption Text=""TopRight"" Value=""BitSnackBarPosition.TopRight"" />
    <BitChoiceGroupOption Text=""BottomLeft"" Value=""BitSnackBarPosition.BottomLeft"" />
    <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackBarPosition.BottomCenter"" />
    <BitChoiceGroupOption Text=""BottomRight"" Value=""BitSnackBarPosition.BottomRight"" />
</BitChoiceGroup>

<div>
    <BitTextField @bind-Value=""BasicSnackBarTitle"" Label=""Title"" />
    <BitTextField @bind-Value=""BasicSnackBarBody"" Label=""Body"" IsMultiline=""true"" Rows=""6"" />
</div>

<div>
    <BitToggle @bind-Value=""BasicSnackBarAutoDismiss"" Label=""Auto Dismiss"" />
    <BitNumericTextField @bind-Value=""BasicSnackBarDismissSeconds"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />
</div>

<BitButton Style=""margin-top: 20px;"" OnClick=""OpenBasicSnackBar"">Show</BitButton>";
    private readonly string example1CsharpCode = @"
private BitSnackBar BasicSnackBarRef = new();
private BitSnackBarType BasicSnackBarType = BitSnackBarType.Info;
private BitSnackBarPosition BasicSnackBarPosition = BitSnackBarPosition.BottomRight;
private string BasicSnackBarTitle = string.Empty;
private string? BasicSnackBarBody;
private bool BasicSnackBarAutoDismiss = true;
private int BasicSnackBarDismissSeconds = 3;

private async Task OpenBasicSnackBar()
{
    await BasicSnackBarRef.Show(BasicSnackBarTitle, BasicSnackBarBody, BasicSnackBarType);
}";

    private readonly string example2RazorCode = @"
<BitSnackBar @ref=""DismissIconName"" DismissIconName=""@BitIconName.Go"" />
<BitButton OnClick=""OpenDismissIconName"">Dismiss Icon Name</BitButton>

<BitSnackBar @ref=""TitleTemplate"" AutoDismiss=""false"">
    <TitleTemplate Context=""title"">
        <div style=""display: flex; flex-direction: row; gap: 10px;"">
            <span>@title</span>
            <BitProgressIndicator BarHeight=""20"" Style=""width: 40px;"" />
        </div>
    </TitleTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenTitleTemplate"">Title Template</BitButton>

<BitSnackBar @ref=""BodyTemplate"" AutoDismiss=""false"">
    <BodyTemplate Context=""body"">
        <div style=""display: flex; flex-flow: column nowrap; gap: 5px;"">
            <span style=""font-size: 12px; margin-bottom: 5px;"">@body</span>
            <div style=""display: flex; gap: 10px;"">
                <BitButton OnClick=""@(() => BodyTemplateAnswer = ""Yes"")"">Yes</BitButton>
                <BitButton OnClick=""@(() => BodyTemplateAnswer = ""No"")"">No</BitButton>
            </div>
            <span>Answer: @BodyTemplateAnswer</span>
        </div>
    </BodyTemplate>
</BitSnackBar>
<BitButton OnClick=""OpenBodyTemplate"">Body Template</BitButton>";
    private readonly string example2CsharpCode = @"
private BitSnackBar DismissIconName = new();
private BitSnackBar TitleTemplate = new();
private BitSnackBar BodyTemplate = new();

private string? BodyTemplateAnswer;

private async Task OpenDismissIconName()
{
    await DismissIconName.Success(""This is title"", ""This is body"");
}

private async Task OpenTitleTemplate()
{
    await TitleTemplate.Warning(""This is title"", ""This is body"");
}

private async Task OpenBodyTemplate()
{
    await BodyTemplate.Error(""This is title"", ""This is body"");
}";

    private readonly string example3RazorCode = @"
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


<BitSnackBar @ref=""SnackBarStyle"" Style=""background-color: dodgerblue; border-radius: 0.5rem;"" />
<BitButton OnClick=""OpenSnackBarStyle"">Custom style</BitButton>

<BitSnackBar @ref=""SnackBarClass"" Class=""custom-class"" />
<BitButton OnClick=""OpenSnackBarClass"">Custom class</BitButton>
                    

<BitSnackBar @ref=""SnackBarStyles""
             Styles=""@(new() { Container = ""width: 16rem; background-color: purple;"",
                               Header = ""background-color: rebeccapurple; padding: 0.2rem;"" })"" />
<BitButton OnClick=""OpenSnackBarStyles"">Custom styles</BitButton>
                    
<BitSnackBar @ref=""SnackBarClasses"" 
             Classes=""@(new() { Container = ""custom-container"",
                                Progress = ""custom-progress"" })"" />
<BitButton OnClick=""OpenSnackBarClasses"">Custom classes</BitButton>";
    private readonly string example3CsharpCode = @"
private BitSnackBar SnackBarStyle = new();
private BitSnackBar SnackBarClass = new();
private BitSnackBar SnackBarStyles = new();
private BitSnackBar SnackBarClasses = new();

private async Task OpenSnackBarStyle()
{
    await SnackBarStyle.Show(""This is title"", ""This is body"", null);
}

private async Task OpenSnackBarClass()
{
    await SnackBarClass.Show(""This is title"", ""This is body"", null);
}

private async Task OpenSnackBarStyles()
{
    await SnackBarStyles.Show(""This is title"", ""This is body"");
}

private async Task OpenSnackBarClasses()
{
    await SnackBarClasses.Show(""This is title"", ""This is body"");
}";
}
