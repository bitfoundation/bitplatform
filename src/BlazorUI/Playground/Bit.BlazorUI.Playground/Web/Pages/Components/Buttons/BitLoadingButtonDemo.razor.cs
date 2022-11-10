using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Buttons;

public partial class BitLoadingButtonDemo
{
    private bool Example1Toggle;
    private bool Example2Toggle;
    private bool Example3Toggle;
    private bool Example4Toggle;
    private bool Example5Toggle;

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

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            Description = "Whether the icon button can have focus in disabled mode."
        },
        new ComponentParameter
        {
            Name = "AriaDescription",
            Type = "string?",
            Description = "Detailed description of the icon button for the benefit of screen readers."
        },
        new ComponentParameter
        {
            Name = "AriaHidden",
            Type = "bool",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
        },
        new ComponentParameter
        {
            Name = "ButtonType",
            Type = "BitButtonType?",
            Description = "The type of the button.",
            LinkType = LinkType.Link,
            Href = "#button-type-enum"
        },
        new ComponentParameter
        {
            Name = "ButtonStyle",
            Type = "BitButtonStyle",
            DefaultValue = "BitButtonStyle.Primary",
            Description = "The style of button, Possible values: Primary | Standard.",
            LinkType = LinkType.Link,
            Href = "#button-style-enum"
        },
        new ComponentParameter
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            Description = "The content of button, It can be Any custom tag or a text."
        },
        new ComponentParameter
        {
            Name = "IsLoading",
            Type = "bool",
            Description = "Determine whether the button is in loading mode or not."
        },
        new ComponentParameter
        {
            Name = "LoadingLabel",
            Type = "string?",
            Description = "The loading label to show next to the spinner."
        },
        new ComponentParameter
        {
            Name = "LoadingSpinnerSize",
            Type = "BitSpinnerSize",
            DefaultValue = "BitSpinnerSize.Small",
            Description = "The size of loading spinner to render.",
            LinkType = LinkType.Link,
            Href = "#spinner-size-enum"
        },
        new ComponentParameter
        {
            Name = "LoadingLabelPosition",
            Type = "BitSpinnerLabelPosition",
            DefaultValue = "BitSpinnerLabelPosition.Right",
            Description = "The position of the loading Label in regards to the spinner animation.",
            LinkType = LinkType.Link,
            Href = "#spinner-position-enum"
        },
        new ComponentParameter
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the content inside the Button in the Loading state.",
        },
        new ComponentParameter
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the button clicked."
        },
        new ComponentParameter
        {
            Name = "Title",
            Type = "string?",
            Description = "The tooltip to show when the mouse is placed on the icon button."
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "button-style-enum",
            Title = "BitButtonStyle Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Primary",
                    Description="The button with white text on a blue background.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Standard",
                    Description="The button with black text on a white background.",
                    Value="1",
                }
            }
        },
        new EnumParameter()
        {
            Id = "button-type-enum",
            Title = "BitButtonType Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        },
        new EnumParameter()
        {
            Id = "spinner-size-enum",
            Title = "BitSpinnerLabelPosition Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Top",
                    Description="The label shows on the top of the spinner.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Right",
                    Description="The label shows on the right side of the spinner.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Bottom",
                    Description="The label shows on the bottom of the spinner.",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "Left",
                    Description="The label shows on the left side of the spinner.",
                    Value="3",
                },
            }
        },
        new EnumParameter()
        {
            Id = "spinner-position-enum",
            Title = "BitSpinnerSize Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Medium",
                    Description="20px Spinner diameter.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Large",
                    Description="28px Spinner diameter.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Small",
                    Description="16px Spinner diameter.",
                    Value="2",
                },
                new EnumItem()
                {
                    Name= "XSmall",
                    Description="12px Spinner diameter.",
                    Value="3",
                },
            }
        },
    };

    #region Example Code 1

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
";

    private readonly string example1CSharpCode = @"
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
";

    #endregion

    #region Example Code 2

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
";

    private readonly string example2CSharpCode = @"
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
";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<BitLoadingButton IsLoading=""TopPositionIsLoading""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitSpinnerLabelPosition.Top""
                  OnClick=""TopPositionOnClick"">
    Top (@TopPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""RightPositionIsLoading""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitSpinnerLabelPosition.Right""
                  OnClick=""RightPositionOnClick"">
    Right (@RightPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""BottomPositionIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitSpinnerLabelPosition.Bottom""
                  OnClick=""BottomPositionOnClick"">
    Bottom (@BottomPositionCounter)
</BitLoadingButton>

<BitLoadingButton IsLoading=""LeftPositionIsLoading""
                  ButtonStyle=""BitButtonStyle.Standard""
                  LoadingLabel=""Loading...""
                  LoadingLabelPosition=""BitSpinnerLabelPosition.Left""
                  OnClick=""LeftPositionOnClick"">
    Left (@LeftPositionCounter)
</BitLoadingButton>
";

    private readonly string example3CSharpCode = @"
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
";

    #endregion

    #region Example Code 4

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
";

    private readonly string example4CSharpCode = @"
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
";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
<style>
    .custom-loading {
        display: flex;
        justify-content: center;
        align-items: center;
        gap: rem(5px);
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
";

    private readonly string example5CSharpCode = @"
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
";

    #endregion
}
