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



    private readonly string example1HTMLCode = @"
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
    private readonly string example1CSharpCode = @"
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

    private readonly string example2HTMLCode = @"
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
    private readonly string example2CSharpCode = @"
private BitSnackBar DismissIconName = new();
private BitSnackBar TitleTemplate = new();
private BitSnackBar BodyTemplate = new();
private string? BodyTemplateAnswer;";
}
