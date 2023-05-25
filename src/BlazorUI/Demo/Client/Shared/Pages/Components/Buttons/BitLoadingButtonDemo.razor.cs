using Bit.BlazorUI.Demo.Client.Shared.Models;
using Bit.BlazorUI.Demo.Client.Shared.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Shared.Pages.Components.Buttons;

public partial class BitLoadingButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            Description = "Whether the icon button can have focus in disabled mode."
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the icon button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new()
        {
            Name = "ButtonType",
            Type = "BitButtonType?",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum"
        },
        new()
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum"
        },
        new()
        {
            Name = "ButtonSize",
            Type = "BitButtonSize",
            DefaultValue = "BitButtonSize.Medium",
            Description = "The size of button, Possible values: Small | Medium | Large.",
            LinkType = LinkType.Link,
            Href = "#button-size-enum"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of button, It can be Any custom tag or a text."
        },
        new()
        {
            Name = "IsLoading",
            Type = "bool",
            Description = "Determine whether the button is in loading mode or not."
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            Description = "The loading label to show next to the spinner."
        },
        new()
        {
            Name = "LoadingSpinnerSize",
            Type = "BitSpinnerSize",
            DefaultValue = "BitSpinnerSize.Small",
            Description = "The size of loading spinner to render.",
            LinkType = LinkType.Link,
            Href = "#spinner-size-enum"
        },
        new()
        {
            Name = "LoadingLabelPosition",
            Type = "BitLabelPosition",
            DefaultValue = "BitLabelPosition.Right",
            Description = "The position of the loading Label in regards to the spinner animation.",
            LinkType = LinkType.Link,
            Href = "#spinner-position-enum"
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the content inside the Button in the Loading state.",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the button clicked."
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            Description = "The tooltip to show when the mouse is placed on the icon button."
        },
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "button-type-enum",
            Name = "BitButtonType",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "button-style-enum",
            Name = "BitButtonStyle",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new()
        {
            Id = "button-size-enum",
            Name = "BitButtonSize",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
               new()
                {
                    Name= "Small",
                    Description="The button size is small.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The button size is medium.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The button size is large.",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "spinner-size-enum",
            Name = "BitLabelPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spinner.",
                    Value="0",
                },
                new()
                {
                    Name= "Right",
                    Description="The label shows on the right side of the spinner.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spinner.",
                    Value="2",
                },
                new()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spinner.",
                    Value="3",
                },
            }
        },
        new()
        {
            Id = "spinner-position-enum",
            Name = "BitSpinnerSize",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new()
                {
                    Name= "Medium",
                    Description="20px Spinner diameter.",
                    Value="0",
                },
                new()
                {
                    Name= "Large",
                    Description="28px Spinner diameter.",
                    Value="1",
                },
                new()
                {
                    Name= "Small",
                    Description="16px Spinner diameter.",
                    Value="2",
                },
                new()
                {
                    Name= "XSmall",
                    Description="12px Spinner diameter.",
                    Value="3",
                },
            }
        },
    };



    private bool Example1Toggle;
    private bool Example2Toggle;
    private bool Example3Toggle;
    private bool Example4Toggle;
    private bool Example5Toggle;
    private bool Example6Toggle;
    private bool Example7Toggle;

    private void Example1ToggleOnChange()
    {
        BasicPrimaryIsLoading = !BasicPrimaryIsLoading;
        BasicStandardIsLoading = !BasicStandardIsLoading;
    }

    private void Example2ToggleOnChange()
    {
        LoadingLabelPrimaryIsLoading = !LoadingLabelPrimaryIsLoading;
        LoadingLabelStandardIsLoading = !LoadingLabelStandardIsLoading;
    }

    private void Example3ToggleOnChange()
    {
        TopPositionIsLoading = !TopPositionIsLoading;
        RightPositionIsLoading = !RightPositionIsLoading;
        BottomPositionIsLoading = !BottomPositionIsLoading;
        LeftPositionIsLoading = !LeftPositionIsLoading;
    }

    private void Example4ToggleOnChange()
    {
        XSmallIsLoading = !XSmallIsLoading;
        SmallIsLoading = !SmallIsLoading;
        MediumIsLoading = !MediumIsLoading;
        LargeIsLoading = !LargeIsLoading;
    }

    private void Example5ToggleOnChange()
    {
        EllipsisIsLoading = !EllipsisIsLoading;
        GridIsLoading = !GridIsLoading;
        RollerIsLoading = !RollerIsLoading;
        SpinnerIsLoading = !SpinnerIsLoading;
    }

    private void Example6ToggleOnChange()
    {
        SmallButtonIsLoading = !SmallButtonIsLoading;
        MediumButtonIsLoading = !MediumButtonIsLoading;
        LargeButtonIsLoading = !LargeButtonIsLoading;
    }

    private void Example7ToggleOnChange()
    {
        CustomizedSmallButtonIsLoading = !CustomizedSmallButtonIsLoading;
        CustomizedMediumButtonIsLoading = !CustomizedMediumButtonIsLoading;
        CustomizedLargeButtonIsLoading = !CustomizedLargeButtonIsLoading;
    }

    #region Basic

    private bool BasicPrimaryIsLoading;
    private int BasicPrimaryCounter;
    private bool BasicStandardIsLoading;
    private int BasicStandardCounter;

    private async Task BasicPrimaryOnClick()
    {
        BasicPrimaryIsLoading = true;
        await Task.Delay(1000);
        BasicPrimaryCounter++;
        BasicPrimaryIsLoading = false;
    }

    private async Task BasicStandardOnClick()
    {
        BasicStandardIsLoading = true;
        await Task.Delay(1000);
        BasicStandardCounter++;
        BasicStandardIsLoading = false;
    }

    #endregion

    #region LoadingLabel

    private bool LoadingLabelPrimaryIsLoading;
    private int LoadingLabelPrimaryCounter;
    private bool LoadingLabelStandardIsLoading;
    private int LoadingLabelStandardCounter;

    private async Task LoadingLabelPrimaryOnClick()
    {
        LoadingLabelPrimaryIsLoading = true;
        await Task.Delay(1000);
        LoadingLabelPrimaryCounter++;
        LoadingLabelPrimaryIsLoading = false;
    }

    private async Task LoadingLabelStandardOnClick()
    {
        LoadingLabelStandardIsLoading = true;
        await Task.Delay(1000);
        LoadingLabelStandardCounter++;
        LoadingLabelStandardIsLoading = false;
    }

    #endregion

    #region LabelPosition

    private bool TopPositionIsLoading;
    private int TopPositionCounter;
    private bool RightPositionIsLoading;
    private int RightPositionCounter;
    private bool BottomPositionIsLoading;
    private int BottomPositionCounter;
    private bool LeftPositionIsLoading;
    private int LeftPositionCounter;

    private async Task TopPositionOnClick()
    {
        TopPositionIsLoading = true;
        await Task.Delay(1000);
        TopPositionCounter++;
        TopPositionIsLoading = false;
    }

    private async Task RightPositionOnClick()
    {
        RightPositionIsLoading = true;
        await Task.Delay(1000);
        RightPositionCounter++;
        RightPositionIsLoading = false;
    }

    private async Task BottomPositionOnClick()
    {
        BottomPositionIsLoading = true;
        await Task.Delay(1000);
        BottomPositionCounter++;
        BottomPositionIsLoading = false;
    }

    private async Task LeftPositionOnClick()
    {
        LeftPositionIsLoading = true;
        await Task.Delay(1000);
        LeftPositionCounter++;
        LeftPositionIsLoading = false;
    }

    #endregion

    #region SpinnerSize

    private bool XSmallIsLoading;
    private int XSmallCounter;
    private bool SmallIsLoading;
    private int SmallCounter;
    private bool MediumIsLoading;
    private int MediumCounter;
    private bool LargeIsLoading;
    private int LargeCounter;

    private async Task XSmallOnClick()
    {
        XSmallIsLoading = true;
        await Task.Delay(1000);
        XSmallCounter++;
        XSmallIsLoading = false;
    }

    private async Task SmallOnClick()
    {
        SmallIsLoading = true;
        await Task.Delay(1000);
        SmallCounter++;
        SmallIsLoading = false;
    }

    private async Task MediumOnClick()
    {
        MediumIsLoading = true;
        await Task.Delay(1000);
        MediumCounter++;
        MediumIsLoading = false;
    }

    private async Task LargeOnClick()
    {
        LargeIsLoading = true;
        await Task.Delay(1000);
        LargeCounter++;
        LargeIsLoading = false;
    }

    #endregion

    #region LoadingTemplate

    private bool EllipsisIsLoading;
    private bool GridIsLoading;
    private bool RollerIsLoading;
    private bool SpinnerIsLoading;

    private async Task EllipsisOnClick()
    {
        EllipsisIsLoading = true;
        await Task.Delay(1000);
        EllipsisIsLoading = false;
    }

    private async Task GridOnClick()
    {
        GridIsLoading = true;
        await Task.Delay(1000);
        GridIsLoading = false;
    }

    private async Task RollerOnClick()
    {
        RollerIsLoading = true;
        await Task.Delay(1000);
        RollerIsLoading = false;
    }

    private async Task SpinnerOnClick()
    {
        SpinnerIsLoading = true;
        await Task.Delay(1000);
        SpinnerIsLoading = false;
    }

    #endregion

    #region Size

    private bool SmallButtonIsLoading;
    private int SmallButtonCounter;
    private bool MediumButtonIsLoading;
    private int MediumButtonCounter;
    private bool LargeButtonIsLoading;
    private int LargeButtonCounter;

    private async Task SmallButtonOnClick()
    {
        SmallButtonIsLoading = true;
        await Task.Delay(1000);
        SmallButtonCounter++;
        SmallButtonIsLoading = false;
    }

    private async Task MediumButtonOnClick()
    {
        MediumButtonIsLoading = true;
        await Task.Delay(1000);
        MediumButtonCounter++;
        MediumButtonIsLoading = false;
    }

    private async Task LargeButtonOnClick()
    {
        LargeButtonIsLoading = true;
        await Task.Delay(1000);
        LargeButtonCounter++;
        LargeButtonIsLoading = false;
    }

    #endregion

    #region Customized Size

    private bool CustomizedSmallButtonIsLoading;
    private int CustomizedSmallButtonCounter;
    private bool CustomizedMediumButtonIsLoading;
    private int CustomizedMediumButtonCounter;
    private bool CustomizedLargeButtonIsLoading;
    private int CustomizedLargeButtonCounter;

    private async Task CustomizedSmallButtonOnClick()
    {
        CustomizedSmallButtonIsLoading = true;
        await Task.Delay(1000);
        CustomizedSmallButtonCounter++;
        CustomizedSmallButtonIsLoading = false;
    }

    private async Task CustomizedMediumButtonOnClick()
    {
        CustomizedMediumButtonIsLoading = true;
        await Task.Delay(1000);
        CustomizedMediumButtonCounter++;
        CustomizedMediumButtonIsLoading = false;
    }

    private async Task CustomizedLargeButtonOnClick()
    {
        CustomizedLargeButtonIsLoading = true;
        await Task.Delay(1000);
        CustomizedLargeButtonCounter++;
        CustomizedLargeButtonIsLoading = false;
    }

    #endregion

    

    private readonly string example1HTMLCode = @"
<BitLoadingButton IsLoading=""BasicPrimaryIsLoading""
                  ButtonStyle=""BitButtonStyle.Primary""
                  OnClick=""BasicPrimaryOnClick"">
    Primary (@BasicPrimaryCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""BasicStandardIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  OnClick=""BasicStandardOnClick"">
    Standard (@BasicStandardCounter)
</BitLoadingButton>

<BitLoadingButton IsEnabled=""false"">
    Disabled
</BitLoadingButton>

<BitToggle @bind-Value=""Example1Toggle"" OnChange=""Example1ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example1CSharpCode = @"
private bool BasicPrimaryIsLoading;
private int BasicPrimaryCounter;
private bool BasicStandardIsLoading;
private int BasicStandardCounter;
private bool Example1Toggle;

private async Task BasicPrimaryOnClick()
{
    BasicPrimaryIsLoading = true;
    await Task.Delay(1000);
    BasicPrimaryCounter++;
    BasicPrimaryIsLoading = false;
}

private async Task BasicStandardOnClick()
{
    BasicStandardIsLoading = true;
    await Task.Delay(1000);
    BasicStandardCounter++;
    BasicStandardIsLoading = false;
}

private void Example1ToggleOnChange()
{
    BasicPrimaryIsLoading = !BasicPrimaryIsLoading;
    BasicStandardIsLoading = !BasicStandardIsLoading;
}";

    private readonly string example2HTMLCode = @"
<BitLoadingButton IsLoading=""LoadingLabelPrimaryIsLoading""
                  LoadingLabel=""Loading...""
                  ButtonStyle=""BitButtonStyle.Primary""
                  OnClick=""LoadingLabelPrimaryOnClick"">
    Primary (@LoadingLabelPrimaryCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""LoadingLabelStandardIsLoading""
                  LoadingLabel=""Loading...""
                  ButtonStyle=""BitButtonStyle.Standard""
                  OnClick=""LoadingLabelStandardOnClick"">
    Standard (@LoadingLabelStandardCounter)
</BitLoadingButton>

<BitToggle @bind-Value=""Example2Toggle"" OnChange=""Example2ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example2CSharpCode = @"
private bool LoadingLabelPrimaryIsLoading;
private int LoadingLabelPrimaryCounter;
private bool LoadingLabelStandardIsLoading;
private int LoadingLabelStandardCounter;
private bool Example2Toggle;

private async Task LoadingLabelPrimaryOnClick()
{
    LoadingLabelPrimaryIsLoading = true;
    await Task.Delay(1000);
    LoadingLabelPrimaryCounter++;
    LoadingLabelPrimaryIsLoading = false;
}

private async Task LoadingLabelStandardOnClick()
{
    LoadingLabelStandardIsLoading = true;
    await Task.Delay(1000);
    LoadingLabelStandardCounter++;
    LoadingLabelStandardIsLoading = false;
}

private void Example2ToggleOnChange()
{
    LoadingLabelPrimaryIsLoading = !LoadingLabelPrimaryIsLoading;
    LoadingLabelStandardIsLoading = !LoadingLabelStandardIsLoading;
}";

    private readonly string example3HTMLCode = @"
<BitLoadingButton IsLoading=""TopPositionIsLoading""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitLabelPosition.Top""
                  OnClick=""TopPositionOnClick"">
    Top (@TopPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""RightPositionIsLoading""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitLabelPosition.Right""
                  OnClick=""RightPositionOnClick"">
    Right (@RightPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""BottomPositionIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitLabelPosition.Bottom""
                  OnClick=""BottomPositionOnClick"">
    Bottom (@BottomPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""LeftPositionIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitLabelPosition.Left""
                  OnClick=""LeftPositionOnClick"">
    Left (@LeftPositionCounter)
</BitLoadingButton>

<BitToggle @bind-Value=""Example3Toggle"" OnChange=""Example3ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example3CSharpCode = @"
private bool TopPositionIsLoading;
private int TopPositionCounter;
private bool RightPositionIsLoading;
private int RightPositionCounter;
private bool BottomPositionIsLoading;
private int BottomPositionCounter;
private bool LeftPositionIsLoading;
private int LeftPositionCounter;
private bool Example3Toggle;

private async Task TopPositionOnClick()
{
    TopPositionIsLoading = true;
    await Task.Delay(1000);
    TopPositionCounter++;
    TopPositionIsLoading = false;
}

private async Task RightPositionOnClick()
{
    RightPositionIsLoading = true;
    await Task.Delay(1000);
    RightPositionCounter++;
    RightPositionIsLoading = false;
}

private async Task BottomPositionOnClick()
{
    BottomPositionIsLoading = true;
    await Task.Delay(1000);
    BottomPositionCounter++;
    BottomPositionIsLoading = false;
}

private async Task LeftPositionOnClick()
{
    LeftPositionIsLoading = true;
    await Task.Delay(1000);
    LeftPositionCounter++;
    LeftPositionIsLoading = false;
}

private void Example3ToggleOnChange()
{
    TopPositionIsLoading = !TopPositionIsLoading;
    RightPositionIsLoading = !RightPositionIsLoading;
    BottomPositionIsLoading = !BottomPositionIsLoading;
    LeftPositionIsLoading = !LeftPositionIsLoading;
}";

    private readonly string example4HTMLCode = @"
<BitLoadingButton IsLoading=""XSmallIsLoading""
                  LoadingSpinnerSize=""BitSpinnerSize.XSmall""
                  OnClick=""XSmallOnClick"">
    XSmall (@XSmallCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""SmallIsLoading""
                  LoadingSpinnerSize=""BitSpinnerSize.Small""
                  OnClick=""SmallOnClick"">
    Small (@SmallCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""MediumIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingSpinnerSize=""BitSpinnerSize.Medium""
                  OnClick=""MediumOnClick"">
    Medium (@MediumCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""LargeIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingSpinnerSize=""BitSpinnerSize.Large""
                  OnClick=""LargeOnClick"">
    Large (@LargeCounter)
</BitLoadingButton>

<BitToggle @bind-Value=""Example4Toggle"" OnChange=""Example4ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example4CSharpCode = @"
private bool XSmallIsLoading;
private int XSmallCounter;
private bool SmallIsLoading;
private int SmallCounter;
private bool MediumIsLoading;
private int MediumCounter;
private bool LargeIsLoading;
private int LargeCounter;
private bool Example4Toggle;

private async Task XSmallOnClick()
{
    XSmallIsLoading = true;
    await Task.Delay(1000);
    XSmallCounter++;
    XSmallIsLoading = false;
}

private async Task SmallOnClick()
{
    SmallIsLoading = true;
    await Task.Delay(1000);
    SmallCounter++;
    SmallIsLoading = false;
}

private async Task MediumOnClick()
{
    MediumIsLoading = true;
    await Task.Delay(1000);
    MediumCounter++;
    MediumIsLoading = false;
}

private async Task LargeOnClick()
{
    LargeIsLoading = true;
    await Task.Delay(1000);
    LargeCounter++;
    LargeIsLoading = false;
}

private void Example4ToggleOnChange()
{
    XSmallIsLoading = !XSmallIsLoading;
    SmallIsLoading = !SmallIsLoading;
    MediumIsLoading = !MediumIsLoading;
    LargeIsLoading = !LargeIsLoading;
}";

    private readonly string example5HTMLCode = @"
<style>
    .custom-loading {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: 0.3125rem;
    }
</style>
<BitLoadingButton IsLoading=""EllipsisIsLoading""
                    Title=""Ellipsis Loading""
                    OnClick=""EllipsisOnClick"">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitEllipsisLoading Size=""20"" />
            <span>wait...</span>
        </div>
    </LoadingTemplate>
    <ChildContent>
        Ellipsis Loading
    </ChildContent>
</BitLoadingButton>

<BitLoadingButton IsLoading=""GridIsLoading""
                    Title=""Grid Loading""
                    OnClick=""GridOnClick"">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitGridLoading Size=""20"" />
            <span>wait...</span>
        </div>
    </LoadingTemplate>
    <ChildContent>
        Grid Loading
    </ChildContent>
</BitLoadingButton>

<BitLoadingButton IsLoading=""RollerIsLoading""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Title=""Roller Loading""
                    OnClick=""RollerOnClick"">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitRollerLoading Size=""20"" Color=""royalblue"" />
            <span>wait...</span>
        </div>
    </LoadingTemplate>
    <ChildContent>
        Roller Loading
    </ChildContent>
</BitLoadingButton>

<BitLoadingButton IsLoading=""SpinnerIsLoading""
                    ButtonStyle=""BitButtonStyle.Standard""
                    Title=""Spinner Loading""
                    OnClick=""SpinnerOnClick"">
    <LoadingTemplate>
        <div class=""custom-loading"">
            <BitSpinnerLoading Size=""20"" Color=""royalblue"" />
            <span>wait...</span>
        </div>
    </LoadingTemplate>
    <ChildContent>
        Spinner Loading
    </ChildContent>
</BitLoadingButton>

<BitToggle @bind-Value=""Example5Toggle"" OnChange=""Example5ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example5CSharpCode = @"
private bool EllipsisIsLoading;
private bool GridIsLoading;
private bool RollerIsLoading;
private bool SpinnerIsLoading;
private bool Example5Toggle;

private async Task EllipsisOnClick()
{
    EllipsisIsLoading = true;
    await Task.Delay(1000);
    EllipsisIsLoading = false;
}

private async Task GridOnClick()
{
    GridIsLoading = true;
    await Task.Delay(1000);
    GridIsLoading = false;
}

private async Task RollerOnClick()
{
    RollerIsLoading = true;
    await Task.Delay(1000);
    RollerIsLoading = false;
}

private async Task SpinnerOnClick()
{
    SpinnerIsLoading = true;
    await Task.Delay(1000);
    SpinnerIsLoading = false;
}

private void Example5ToggleOnChange()
{
    EllipsisIsLoading = !EllipsisIsLoading;
    GridIsLoading = !GridIsLoading;
    RollerIsLoading = !RollerIsLoading;
    SpinnerIsLoading = !SpinnerIsLoading;
}";

    private readonly string example6HTMLCode = @"
<BitLoadingButton IsLoading=""SmallButtonIsLoading""
                  ButtonSize=""BitButtonSize.Small""
                  OnClick=""SmallButtonOnClick"">
    Small (@SmallButtonCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""MediumButtonIsLoading""
                  ButtonSize=""BitButtonSize.Medium""
                  OnClick=""MediumButtonOnClick"">
    Medium (@MediumButtonCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""LargeButtonIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  ButtonSize=""BitButtonSize.Large""
                  OnClick=""LargeButtonOnClick"">
    Large (@LargeButtonCounter)
</BitLoadingButton>

<BitToggle @bind-Value=""Example6Toggle"" OnChange=""Example6ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example6CSharpCode = @"
private bool SmallButtonIsLoading;
private int SmallButtonCounter;
private bool MediumButtonIsLoading;
private int MediumButtonCounter;
private bool LargeButtonIsLoading;
private int LargeButtonCounter;
private bool Example6Toggle;

private async Task SmallButtonOnClick()
{
    SmallButtonIsLoading = true;
    await Task.Delay(1000);
    SmallButtonCounter++;
    SmallButtonIsLoading = false;
}

private async Task MediumButtonOnClick()
{
    MediumButtonIsLoading = true;
    await Task.Delay(1000);
    MediumButtonCounter++;
    MediumButtonIsLoading = false;
}

private async Task LargeButtonOnClick()
{
    LargeButtonIsLoading = true;
    await Task.Delay(1000);
    LargeButtonCounter++;
    LargeButtonIsLoading = false;
}

private void Example6ToggleOnChange()
{
    SmallButtonIsLoading = !SmallButtonIsLoading;
    MediumButtonIsLoading = !MediumButtonIsLoading;
    LargeButtonIsLoading = !LargeButtonIsLoading;
}";

    private readonly string example7HTMLCode = @"
<style>
    .custom-btn-sm {
        max-width: 7rem;
        width: 7rem;
        padding: 1rem 1.25rem;
        font-size: 1rem;
        line-height: 1.5;
        border-radius: 0.1875rem;
    }

    .custom-btn-md {
        max-width: 10rem;
        width: 10rem;
        padding: 1.125rem 1.5rem;
        font-size: 1.125rem;
        line-height: 1.4;
        border-radius: 0.25rem;
    }

    .custom-btn-lg {
        max-width: 12.5rem;
        width: 12.5rem;
        padding: 1.375rem 1.625rem;
        font-size: 1.3125rem;
        line-height: 1.33;
        border-radius: 0.375rem;
    }
</style>

<BitLoadingButton Class=""custom-btn-sm""
                  ButtonSize=""BitButtonSize.Small""
                  IsLoading=""CustomizedSmallButtonIsLoading""
                  OnClick=""CustomizedSmallButtonOnClick"">
    Small (@CustomizedSmallButtonCounter)
</BitLoadingButton>

<BitLoadingButton Class=""custom-btn-md""
                  ButtonSize=""BitButtonSize.Medium""
                  IsLoading=""CustomizedMediumButtonIsLoading""
                  OnClick=""CustomizedMediumButtonOnClick"">
    Medium (@CustomizedMediumButtonCounter)
</BitLoadingButton>

<BitLoadingButton Class=""custom-btn-lg""
                  ButtonSize=""BitButtonSize.Large""
                  IsLoading=""CustomizedLargeButtonIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  OnClick=""CustomizedLargeButtonOnClick"">
    Large (@CustomizedLargeButtonCounter)
</BitLoadingButton>

<BitToggle @bind-Value=""Example7Toggle"" OnChange=""Example7ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example7CSharpCode = @"
private bool CustomizedSmallButtonIsLoading;
private int CustomizedSmallButtonCounter;
private bool CustomizedMediumButtonIsLoading;
private int CustomizedMediumButtonCounter;
private bool CustomizedLargeButtonIsLoading;
private int CustomizedLargeButtonCounter;
private bool Example7Toggle;

private async Task CustomizedSmallButtonOnClick()
{
    CustomizedSmallButtonIsLoading = true;
    await Task.Delay(1000);
    CustomizedSmallButtonCounter++;
    CustomizedSmallButtonIsLoading = false;
}

private async Task CustomizedMediumButtonOnClick()
{
    CustomizedMediumButtonIsLoading = true;
    await Task.Delay(1000);
    CustomizedMediumButtonCounter++;
    CustomizedMediumButtonIsLoading = false;
}

private async Task CustomizedLargeButtonOnClick()
{
    CustomizedLargeButtonIsLoading = true;
    await Task.Delay(1000);
    CustomizedLargeButtonCounter++;
    CustomizedLargeButtonIsLoading = false;
}

private void Example7ToggleOnChange()
{
    CustomizedSmallButtonIsLoading = !CustomizedSmallButtonIsLoading;
    CustomizedMediumButtonIsLoading = !CustomizedMediumButtonIsLoading;
    CustomizedLargeButtonIsLoading = !CustomizedLargeButtonIsLoading;
}";
}
