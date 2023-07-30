using Bit.BlazorUI.Demo.Client.Core.Models;
using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitLoadingButtonDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AllowDisabledFocus",
            Type = "bool",
            DefaultValue = "true",
            Description = "Whether the icon button can have focus in disabled mode."
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the icon button for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
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
            Name = "ButtonType",
            Type = "BitButtonType?",
            LinkType = LinkType.Link,
            Href = "#button-type-enum",
            DefaultValue = "null",
            Description = "The type of the button."
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
            DefaultValue = "false",
            Description = "Determine whether the button is in loading mode or not."
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The loading label to show next to the spinner."
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
            DefaultValue = "null",
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
            DefaultValue = "null",
            Description = "The tooltip to show when the mouse is placed on the icon button."
        },
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
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

    private void Example1ToggleOnChange()
    {
        BasicPrimaryIsLoading = Example1Toggle;
        BasicStandardIsLoading = Example1Toggle;
    }

    private void Example2ToggleOnChange()
    {
        StyleClassPrimaryIsLoading = Example2Toggle;
        StyleClassStandardIsLoading = Example2Toggle;
    }

    private void Example3ToggleOnChange()
    {
        LoadingLabelPrimaryIsLoading = Example3Toggle;
        LoadingLabelStandardIsLoading = Example3Toggle;
    }

    private void Example4ToggleOnChange()
    {
        TopPositionIsLoading = Example4Toggle;
        RightPositionIsLoading = Example4Toggle;
        BottomPositionIsLoading = Example4Toggle;
        LeftPositionIsLoading = Example4Toggle;
    }

    private void Example5ToggleOnChange()
    {
        EllipsisIsLoading = Example5Toggle;
        RollerIsLoading = Example5Toggle;
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

    #region StyleClass

    private bool StyleClassPrimaryIsLoading;
    private int StyleClassPrimaryCounter;
    private bool StyleClassStandardIsLoading;
    private int StyleClassStandardCounter;

    private async Task StyleClassPrimaryOnClick()
    {
        StyleClassPrimaryIsLoading = true;
        await Task.Delay(1000);
        StyleClassPrimaryCounter++;
        StyleClassPrimaryIsLoading = false;
    }

    private async Task StyleClassStandardOnClick()
    {
        StyleClassStandardIsLoading = true;
        await Task.Delay(1000);
        StyleClassStandardCounter++;
        StyleClassStandardIsLoading = false;
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

    #region LoadingTemplate

    private bool EllipsisIsLoading;
    private bool RollerIsLoading;

    private async Task EllipsisOnClick()
    {
        EllipsisIsLoading = true;
        await Task.Delay(1000);
        EllipsisIsLoading = false;
    }

    private async Task RollerOnClick()
    {
        RollerIsLoading = true;
        await Task.Delay(1000);
        RollerIsLoading = false;
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

<BitLoadingButton IsEnabled=""false"">Disabled</BitLoadingButton>

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
    BasicPrimaryIsLoading = Example1Toggle;
    BasicStandardIsLoading = Example1Toggle;
}";

    private readonly string example2HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }
</style>

<BitLoadingButton IsLoading=""StyleClassPrimaryIsLoading""
                  Style=""color:darkblue; font-weight:bold""
                  OnClick=""StyleClassPrimaryOnClick"">
    Styled Button
</BitLoadingButton>

<BitLoadingButton IsLoading=""StyleClassStandardIsLoading""
                  Class=""custom-class""
                  ButtonStyle=""BitButtonStyle.Standard""
                  OnClick=""StyleClassStandardOnClick"">
    Classed Button
</BitLoadingButton>

<BitToggle @bind-Value=""Example2Toggle"" OnChange=""Example2ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example2CSharpCode = @"
private bool StyleClassPrimaryIsLoading;
private int StyleClassPrimaryCounter;
private bool StyleClassStandardIsLoading;
private int StyleClassStandardCounter;
private bool Example2Toggle;

private async Task StyleClassPrimaryOnClick()
{
    StyleClassPrimaryIsLoading = true;
    await Task.Delay(1000);
    StyleClassPrimaryCounter++;
    StyleClassPrimaryIsLoading = false;
}

private async Task StyleClassStandardOnClick()
{
    StyleClassStandardIsLoading = true;
    await Task.Delay(1000);
    StyleClassStandardCounter++;
    StyleClassStandardIsLoading = false;
}

private void Example1ToggleOnChange()
{
    StyleClassPrimaryIsLoading = Example2Toggle;
    StyleClassStandardIsLoading = Example2Toggle;
}";

    private readonly string example3HTMLCode = @"
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

<BitToggle @bind-Value=""Example2Toggle"" OnChange=""Example3ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example3CSharpCode = @"
private bool LoadingLabelPrimaryIsLoading;
private int LoadingLabelPrimaryCounter;
private bool LoadingLabelStandardIsLoading;
private int LoadingLabelStandardCounter;
private bool Example3Toggle;

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

private void Example3ToggleOnChange()
{
    LoadingLabelPrimaryIsLoading = Example3Toggle;
    LoadingLabelStandardIsLoading = Example3Toggle;
}";

    private readonly string example4HTMLCode = @"
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

<BitToggle @bind-Value=""Example3Toggle"" OnChange=""Example4ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example4CSharpCode = @"
private bool TopPositionIsLoading;
private int TopPositionCounter;
private bool RightPositionIsLoading;
private int RightPositionCounter;
private bool BottomPositionIsLoading;
private int BottomPositionCounter;
private bool LeftPositionIsLoading;
private int LeftPositionCounter;
private bool Example4Toggle;

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

private void Example4ToggleOnChange()
{
    TopPositionIsLoading = Example4Toggle;
    RightPositionIsLoading = Example4Toggle;
    BottomPositionIsLoading = Example4Toggle;
    LeftPositionIsLoading = Example4Toggle;
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

<BitToggle @bind-Value=""Example5Toggle"" OnChange=""Example5ToggleOnChange"" OnText=""Turn Loading Off"" OffText=""Turn Loading On"" />";
    private readonly string example5CSharpCode = @"
private bool EllipsisIsLoading;
private bool RollerIsLoading;
private bool Example5Toggle;

private async Task EllipsisOnClick()
{
    EllipsisIsLoading = true;
    await Task.Delay(1000);
    EllipsisIsLoading = false;
}

private async Task RollerOnClick()
{
    RollerIsLoading = true;
    await Task.Delay(1000);
    RollerIsLoading = false;
}

private void Example5ToggleOnChange()
{
    EllipsisIsLoading = Example5Toggle;
    RollerIsLoading = Example5Toggle;
}";

    private readonly string example6HTMLCode = @"
@if (formIsValidSubmit is false)
{
    <EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"" novalidate>
        <DataAnnotationsValidator />

        <BitTextField Label=""Required"" IsRequired=""true"" @bind-Value=""buttonValidationModel.RequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.RequiredText"" />

        <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
        <ValidationMessage For=""() => buttonValidationModel.NonRequiredText"" />

        <div>
            <BitLoadingButton ButtonType=""BitButtonType.Submit"">Submit</BitLoadingButton>
            <BitLoadingButton ButtonType=""BitButtonType.Reset"">Reset</BitLoadingButton>
            <BitLoadingButton ButtonType=""BitButtonType.Button"">Button</BitLoadingButton>
        </div>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        The form submitted successfully.
    </BitMessageBar>
}";
    private readonly string example6CSharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;

    public string? NonRequiredText { get; set; }
}

private bool formIsValidSubmit;
private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    formIsValidSubmit = true;

    await Task.Delay(2000);

    buttonValidationModel = new();

    formIsValidSubmit = false;

    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    formIsValidSubmit = false;
}";
}
