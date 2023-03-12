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


    private BitSnackbar SnackbarTypeRef = new();
    private async Task OpenSnackbarType(BitSnackbarType type)
    {
        await SnackbarTypeRef.Show(type, "This is title", "This is body");
    }


    private BitSnackbar SnackbarPositionRef = new();
    private BitSnackbarPosition SnackbarPosition;
    private async Task OpenSnackbarPosition(BitSnackbarPosition position)
    {
        SnackbarPosition = position;
        await SnackbarPositionRef.Show("This is title", "This is body");
    }


    private BitSnackbar NonAutoDismiss = new();
    private BitSnackbar AutoDismissTime = new();
    private async Task OpenNonAutoDismiss()
    {
        await NonAutoDismiss.Show("This is title", "This is body");
    }
    private async Task OpenAutoDismissTime()
    {
        await AutoDismissTime.Show("This is title", "This is body");
    }


    private BitSnackbar DismissIconName = new();
    private BitSnackbar TitleTemplate = new();
    private BitSnackbar BodyTemplate = new();
    private string BodyTemplateYesAnswer = "Yes";
    private string BodyTemplateNoAnswer = "No";
    private string? BodyTemplateAnswer;
    private async Task OpenTitleTemplate()
    {
        await TitleTemplate.Show("This is title", "This is body");
    }
    private async Task OpenBodyTemplate()
    {
        await BodyTemplate.Show("This is title", "This is body");
    }
    private async Task OpenDismissIconName()
    {
        await DismissIconName.Show("This is title", "This is body");
    }

    private readonly string example1HTMLCode = @"
<div>
    <BitButton OnClick=""OpenNormalType"">Info</BitButton>
    <BitSnackbar @ref=""NormalType"" Type=""BitSnackbarType.Info"" />
</div>

<div>
    <BitButton OnClick=""OpenSuccessType"">Success</BitButton>
    <BitSnackbar @ref=""SuccessType"" Type=""BitSnackbarType.Success"" />
</div>

<div>
    <BitButton OnClick=""OpenWarningType"">Warning</BitButton>
    <BitSnackbar @ref=""WarningType"" Type=""BitSnackbarType.Warning"" />
</div>

<div>
    <BitButton OnClick=""OpenErrorType"">Error</BitButton>
    <BitSnackbar @ref=""ErrorType"" Type=""BitSnackbarType.Error"" />
</div>
";

    private readonly string example1CSharpCode = @"
private BitSnackbar NormalType;
private BitSnackbar SuccessType;
private BitSnackbar WarningType;
private BitSnackbar ErrorType;

private async void OpenNormalType()
{
    await NormalType.Show(""This is title"", ""This is body"");
}

private async void OpenSuccessType()
{
    await SuccessType.Show(""This is title"", ""This is body"");
}

private async void OpenWarningType()
{
    await WarningType.Show(""This is title"", ""This is body"");
}

private async void OpenErrorType()
{
    await ErrorType.Show(""This is title"", ""This is body"");
}
";

    private readonly string example2HTMLCode = @"
<div>
    <BitButton OnClick=""OpenTopLeftPosition"">Top-Left</BitButton>
    <BitSnackbar @ref=""TopLeftPosition"" Type=""BitSnackbarType.Info"" Position=""BitSnackbarPosition.TopLeft"" />
</div>

<div>
    <BitButton OnClick=""OpenTopCenterPosition"">Top-Center</BitButton>
    <BitSnackbar @ref=""TopCenterPosition"" Type=""BitSnackbarType.Success"" Position=""BitSnackbarPosition.TopCenter"" />
</div>

<div>
    <BitButton OnClick=""OpenTopRightPosition"">Top-Right</BitButton>
    <BitSnackbar @ref=""TopRightPosition"" Type=""BitSnackbarType.Warning"" Position=""BitSnackbarPosition.TopRight"" />
</div>

<div>
    <BitButton OnClick=""OpenBottomLeftPosition"">Bottom-Left</BitButton>
    <BitSnackbar @ref=""BottomLeftPosition"" Type=""BitSnackbarType.Info"" Position=""BitSnackbarPosition.BottomLeft"" />
</div>

<div>
    <BitButton OnClick=""OpenBottomCenterPosition"">Bottom-Center</BitButton>
    <BitSnackbar @ref=""BottomCenterPosition"" Type=""BitSnackbarType.Success"" Position=""BitSnackbarPosition.BottomCenter"" />
</div>

<div>
    <BitButton OnClick=""OpenBottomRightPosition"">Bottom-Right</BitButton>
    <BitSnackbar @ref=""BottomRightPosition"" Type=""BitSnackbarType.Warning"" Position=""BitSnackbarPosition.BottomRight"" />
</div>
";

    private readonly string example2CSharpCode = @"
private BitSnackbar TopLeftPosition;
private BitSnackbar TopCenterPosition;
private BitSnackbar TopRightPosition;
private BitSnackbar BottomLeftPosition;
private BitSnackbar BottomCenterPosition;
private BitSnackbar BottomRightPosition;

private async void OpenTopLeftPosition()
{
    await TopLeftPosition.Show(""This is title"", ""This is body"");
}

private async void OpenTopCenterPosition()
{
    await TopCenterPosition.Show(""This is title"", ""This is body"");
}

private async void OpenTopRightPosition()
{
    await TopRightPosition.Show(""This is title"", ""This is body"");
}

private async void OpenBottomLeftPosition()
{
    await BottomLeftPosition.Show(""This is title"", ""This is body"");
}

private async void OpenBottomCenterPosition()
{
    await BottomCenterPosition.Show(""This is title"", ""This is body"");
}

private async void OpenBottomRightPosition()
{
    await BottomRightPosition.Show(""This is title"", ""This is body"");
}
";

    private readonly string example3HTMLCode = @"
<div>
    <BitButton OnClick=""OpenNonAutoDismiss"">Non-AutoDismiss</BitButton>
    <BitSnackbar @ref=""NonAutoDismiss"" AutoDismiss=""false"" />
</div>

<div>
    <BitButton OnClick=""OpenAutoDismissTime"">AutoDismissTime (10 Seconds)</BitButton>
    <BitSnackbar @ref=""AutoDismissTime"" AutoDismissTime=""TimeSpan.FromSeconds(10)"" />
</div>
";

    private readonly string example3CSharpCode = @"
private BitSnackbar NonAutoDismiss;
private BitSnackbar AutoDismissTime;

private async void OpenNonAutoDismiss()
{
    await NonAutoDismiss.Show(""This is title"", ""This is body"");
}

private async void OpenAutoDismissTime()
{
    await AutoDismissTime.Show(""This is title"", ""This is body"");
}
";

    private readonly string example4HTMLCode = @"
<div>
    <BitButton OnClick=""OpenDismissIconName"">DismissIconName</BitButton>
    <BitSnackbar @ref=""DismissIconName"" DismissIconName=""BitIconName.Go"" />
</div>

<div>
    <BitButton OnClick=""OpenTitleTemplate"">TitleTemplate</BitButton>
    <BitSnackbar @ref=""TitleTemplate"" AutoDismiss=""false"">
        <TitleTemplate Context=""title"">
            <div style=""display: flex; flex-direction: row; gap: 10px;"">
                <span>@title</span>
                <BitProgressIndicator BarHeight=""20"" Style=""width: 40px;"" />
            </div>
        </TitleTemplate>
    </BitSnackbar>
</div>

<div>
    <BitButton OnClick=""OpenBodyTemplate"">BodyTemplate</BitButton>
    <BitSnackbar @ref=""BodyTemplate"" AutoDismiss=""false"">
        <BodyTemplate Context=""body"">
            <span style=""font-size: 12px; margin-bottom: 5px;"">@body</span>
            <div style=""display: flex; gap: 10px;"">
                <BitButton>Yes</BitButton>
                <BitButton>No</BitButton>
            </div>
        </BodyTemplate>
    </BitSnackbar>
</div>
";

    private readonly string example4CSharpCode = @"
private BitSnackbar DismissIconName;
private BitSnackbar TitleTemplate;
private BitSnackbar BodyTemplate;

private async void OpenTitleTemplate()
{
    await TitleTemplate.Show(""This is title"", ""This is body"");
}

private async void OpenBodyTemplate()
{
    await BodyTemplate.Show(""This is title"", ""This is body"");
}

private async void OpenDismissIconName()
{
    await DismissIconName.Show(""This is title"", ""This is body"");
}
";
}
