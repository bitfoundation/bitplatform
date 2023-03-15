using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Snackbar;

public partial class BitSnackbarDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AutoDismiss",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether or not to dismiss itself automatically.",
        },
        new ComponentParameter()
        {
            Name = "AutoDismissTime",
            Type = "TimeSpan",
            DefaultValue = "TimeSpan.FromSeconds(3)",
            Description = "How long the Snackbar will automatically dismiss (default is 3 seconds).",
        },
        new ComponentParameter()
        {
            Name = "BodyTemplate",
            Type = "RenderFragment<string>?",
            Description = "Used to customize how content inside the Body is rendered.",
        },
        new ComponentParameter()
        {
            Name = "DismissIconName",
            Type = "BitIconName?",
            Description = "Dismiss Icon in Snackbar.",
        },
        new ComponentParameter()
        {
            Name = "OnDismiss",
            Type = "EventCallback",
            Description = "Callback for when the Dismissed.",
        },
        new ComponentParameter()
        {
            Name = "Position",
            Type = "BitSnackbarPosition",
            DefaultValue = "BitSnackbarPosition.BottomRight",
            Description = "The position of Snackbar to show.",
            LinkType = LinkType.Link,
            Href = "snackbar-position-enum"
        },
        new ComponentParameter()
        {
            Name = "TitleTemplate",
            Type = "RenderFragment<string>?",
            Description = "Used to customize how content inside the Title is rendered. ",
        },
    };
    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "snackbar-position-enum",
            Title = "BitSnackbarPosition Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Center",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "TopCenter",
                    Value = "1",
                },
                new EnumItem()
                {
                    Name = "TopRight",
                    Value = "2",
                },
                new EnumItem()
                {
                    Name = "TopLeft",
                    Value = "3",
                },
                new EnumItem()
                {
                    Name = "BottomCenter",
                    Value = "4",
                },
                new EnumItem()
                {
                    Name = "BottomRight",
                    Value = "5",
                },
                new EnumItem()
                {
                    Name = "BottomLeft",
                    Value = "6",
                },
            }
        },
        new EnumParameter()
        {
            Id = "snackbar-type-enum",
            Title = "BitSnackbarType Enum",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name = "Info",
                    Description = "Info styled Snackbar",
                    Value = "0",
                },
                new EnumItem()
                {
                    Name = "Success",
                    Description = "Success styled Snackbar",
                    Value = "1",
                },
                new EnumItem()
                {
                    Name = "Warning",
                    Description = "Warning styled Snackbar",
                    Value = "2",
                },
                new EnumItem()
                {
                    Name = "Error",
                    Description = "Error styled Snackbar",
                    Value = "3",
                },
            }
        },
    };


    private BitSnackbar BasicSnackbarRef = new();
    private BitSnackbarType BasicSnackbarType = BitSnackbarType.Info;
    private BitSnackbarPosition BasicSnackbarPosition = BitSnackbarPosition.BottomRight;
    private string BasicSnackbarTitle = string.Empty;
    private string? BasicSnackbarBody;
    private bool BasicSnackbarAutoDismiss = true;
    private int BasicSnackbarDissmissSeconds = 3;
    private async Task OpenBasicSnackbar()
    {
        await BasicSnackbarRef.Show(BasicSnackbarTitle, BasicSnackbarBody, BasicSnackbarType);
    }


    private BitSnackbar DismissIconName = new();
    private BitSnackbar TitleTemplate = new();
    private BitSnackbar BodyTemplate = new();
    private string BodyTemplateYesAnswer = "Yes";
    private string BodyTemplateNoAnswer = "No";
    private string? BodyTemplateAnswer;
    private async Task OpenTitleTemplate()
    {
        await TitleTemplate.Warning("This is title", "This is body");
    }
    private async Task OpenBodyTemplate()
    {
        await BodyTemplate.Error("This is title", "This is body");
    }
    private async Task OpenDismissIconName()
    {
        await DismissIconName.Success("This is title", "This is body");
    }

    private readonly string example1HTMLCode = @"
<style>
    .example-box {
        display: flex;
        flex-flow: row wrap;
        gap: 20px;
    }
</style>

<div class=""example-box"">
    <BitSnackbar @ref=""BasicSnackbarRef""
                    Position=""@BasicSnackbarPosition""
                    AutoDismiss=""@BasicSnackbarAutoDismiss""
                    AutoDismissTime=""TimeSpan.FromSeconds(BasicSnackbarDissmissSeconds)"" />

    <BitChoiceGroup @bind-Value=""BasicSnackbarType"" Label=""Type"" TItem=""BitChoiceGroupOption<BitSnackbarType>"" TValue=""BitSnackbarType"">
        <BitChoiceGroupOption Text=""Info"" Value=""BitSnackbarType.Info"" />
        <BitChoiceGroupOption Text=""Success"" Value=""BitSnackbarType.Success"" />
        <BitChoiceGroupOption Text=""Warning"" Value=""BitSnackbarType.Warning"" />
        <BitChoiceGroupOption Text=""Error"" Value=""BitSnackbarType.Error"" />
    </BitChoiceGroup>
    <BitChoiceGroup @bind-Value=""BasicSnackbarPosition"" Label=""Position"" TItem=""BitChoiceGroupOption<BitSnackbarPosition>"" TValue=""BitSnackbarPosition"">
        <BitChoiceGroupOption Text=""TopCenter"" Value=""BitSnackbarPosition.TopCenter"" />
        <BitChoiceGroupOption Text=""TopRight"" Value=""BitSnackbarPosition.TopRight"" />
        <BitChoiceGroupOption Text=""TopLeft"" Value=""BitSnackbarPosition.TopLeft"" />
        <BitChoiceGroupOption Text=""BottomCenter"" Value=""BitSnackbarPosition.BottomCenter"" />
        <BitChoiceGroupOption Text=""BottomRight"" Value=""BitSnackbarPosition.BottomRight"" />
        <BitChoiceGroupOption Text=""BottomLeft"" Value=""BitSnackbarPosition.BottomLeft"" />
    </BitChoiceGroup>
    <div>
        <BitTextField @bind-Value=""BasicSnackbarTitle"" Label=""Title"" />
        <BitTextField @bind-Value=""BasicSnackbarBody"" Label=""Body"" IsMultiline=""true"" Rows=""6"" />
    </div>
    <div>
        <BitToggle @bind-Value=""BasicSnackbarAutoDismiss"" Label=""Auto Dismiss"" />
        <BitNumericTextField @bind-Value=""BasicSnackbarDissmissSeconds"" Step=""1"" Min=""1"" Label=""Dismiss Time (based on second)"" />
    </div>
</div>
<BitButton Style=""margin-top: 20px;"" OnClick=""OpenBasicSnackbar"">Show</BitButton>
";

    private readonly string example1CSharpCode = @"
private BitSnackbar BasicSnackbarRef = new();
private BitSnackbarType BasicSnackbarType = BitSnackbarType.Info;
private BitSnackbarPosition BasicSnackbarPosition = BitSnackbarPosition.BottomRight;
private string BasicSnackbarTitle = string.Empty;
private string? BasicSnackbarBody;
private bool BasicSnackbarAutoDismiss = true;
private int BasicSnackbarDissmissSeconds = 3;
private async Task OpenBasicSnackbar()
{
    await BasicSnackbarRef.Show(BasicSnackbarTitle, BasicSnackbarBody, BasicSnackbarType);
}
";

    private readonly string example2HTMLCode = @"
<style>
    .example-box {
        display: flex;
        flex-flow: row wrap;
        gap: 20px;
    }
</style>

<div class=""example-box"">
    <div>
        <BitSnackbar @ref=""DismissIconName"" DismissIconName=""BitIconName.Go"" />
        <BitButton OnClick=""OpenDismissIconName"">Dismiss Icon Name</BitButton>
    </div>

    <div>
        <BitSnackbar @ref=""TitleTemplate"" AutoDismiss=""false"">
            <TitleTemplate Context=""title"">
                <div style=""display: flex; flex-direction: row; gap: 10px;"">
                    <span>@title</span>
                    <BitProgressIndicator BarHeight=""20"" Style=""width: 40px;"" />
                </div>
            </TitleTemplate>
        </BitSnackbar>
        <BitButton OnClick=""OpenTitleTemplate"">Title Template</BitButton>
    </div>

    <div>
        <BitSnackbar @ref=""BodyTemplate"" AutoDismiss=""false"">
            <BodyTemplate Context=""body"">
                <div style=""display: flex; flex-flow: column nowrap; gap: 5px;"">
                    <span style=""font-size: 12px; margin-bottom: 5px;"">@body</span>
                    <div style=""display: flex; gap: 10px;"">
                        <BitButton OnClick=""() => BodyTemplateAnswer = BodyTemplateYesAnswer"">Yes</BitButton>
                        <BitButton OnClick=""() => BodyTemplateAnswer = BodyTemplateNoAnswer"">No</BitButton>
                    </div>
                    <span>Answer: @BodyTemplateAnswer</span>
                </div>
            </BodyTemplate>
        </BitSnackbar>
        <BitButton OnClick=""OpenBodyTemplate"">Body Template</BitButton>
    </div>
</div>
";

    private readonly string example2CSharpCode = @"
private BitSnackbar DismissIconName = new();
private BitSnackbar TitleTemplate = new();
private BitSnackbar BodyTemplate = new();
private string BodyTemplateYesAnswer = ""Yes"";
private string BodyTemplateNoAnswer = ""No"";
private string? BodyTemplateAnswer;
";
}
