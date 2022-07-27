using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup;

public partial class BitChoiceGroupDemo
{
    private List<BitChoiceGroupOption> example7Options;

    /// <summary>
    /// BitChoiceGroupDemo class constructure
    /// </summary>
    public BitChoiceGroupDemo()
    {
        example7Options = new()
        {
            new BitChoiceGroupOption()
            {
               Text = "Option A",
               Value = "A",
               OnChange = () => OptionOnChange("A")
            },
            new BitChoiceGroupOption()
            {
                Text = "Option B",
                Value = "B",
                OnChange = () => OptionOnChange("B")
            },
            new BitChoiceGroupOption()
            {
               Text = "Option C",
               Value = "C",
               OnChange = () => OptionOnChange("C")
            },
            new BitChoiceGroupOption()
            {
               Text = "Option D",
               Value = "D",
               OnChange = () => OptionOnChange("D")
            }
        };
    }

    private List<BitChoiceGroupOption> Example1Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Option A",
            Value = "A"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option B",
            Value = "B"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option C",
            Value = "C"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option D",
            Value = "D"
        }
    };

    private List<BitChoiceGroupOption> Example2Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Option A",
            Value = "A"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option B",
            Value = "B"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option C",
            Value = "C",
            IsEnabled = false
        },
        new BitChoiceGroupOption()
        {
            Text = "Option D",
            Value = "D"
        }
    };

    private List<BitChoiceGroupOption> Example3Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Bar",
            Value = "Bar",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png",
            ImageAlt = "alt for Bar image",
            ImageSize = new Size(32, 32)
        },
        new BitChoiceGroupOption()
        {
            Text = "Pie",
            Value = "Pie",
            ImageSrc= "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png",
            SelectedImageSrc = "https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png",
            ImageAlt = "alt for Pie image",
            ImageSize = new Size(32, 32)
        }
    };

    private List<BitChoiceGroupOption> Example4Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Day",
            Value = "Day",
            IconName = BitIconName.CalendarDay
        },
        new BitChoiceGroupOption()
        {
            Text = "Week",
            Value = "Week",
            IconName = BitIconName.CalendarWeek
        },
        new BitChoiceGroupOption()
        {
            Text = "Month",
            Value = "Month",
            IconName = BitIconName.Calendar,
            IsEnabled = false
        }
    };

    private List<BitChoiceGroupOption> Example5Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Option A",
            Value = "A"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option B",
            Value = "B"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option C",
            Value = "C"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option D",
            Value = "D"
        }
    };

    private List<BitChoiceGroupOption> Example6Options { get; set; } = new()
    {
        new BitChoiceGroupOption()
        {
            Text = "Option A",
            Value = "A"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option B",
            Value = "B"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option C",
            Value = "C"
        },
        new BitChoiceGroupOption()
        {
            Text = "Option D",
            Value = "D"
        }
    };

    public ChoiceGroupValidationModel ValidationModel = new();

    public string SuccessMessage { get; set; } = string.Empty;

    public string OnChangeValue { get; set; } = string.Empty;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "Options",
            Type = "List<BitChoiceGroupOption>",
            DefaultValue = "new List<BitChoiceGroupOption>()",
            Description = "List of options, each of which is a selection in the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "a Guid",
            Description = "Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "DefaultValue",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default selected Value for ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            DefaultValue = "null",
            Description = "ID of an element to use as the aria label for this ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "Descriptive label for the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "LabelFragment",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize the label for the ChoiceGroup."
        },
        new ComponentParameter()
        {
            Name = "ValueChanged",
            Type = "EventCallback<string?>",
            DefaultValue = "",
            Description = "Callback for when the option has been changed.",
        },
        new ComponentParameter()
        {
            Name = "Value",
            Type = "string?",
            DefaultValue = "",
            Description = "Value of ChoiceGroup, the value of selected ChoiceGroupOption set on it.",
        }
    };

    #region Example Code 1

    private readonly string example1HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example1Options"">
</BitChoiceGroup>";

    private readonly string example1CSharpCode = @"
public List<BitChoiceGroupOption> Example1Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Option A"",
        Value = ""A""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option B"",
        Value = ""B""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option C"",
        Value = ""C""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option D"",
        Value = ""D""
    }
}; 
";

    #endregion

    #region Example Code 2

    private readonly string example2CSharpCode = @"
public List<BitChoiceGroupOption> Example2Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Option A"",
        Value = ""A""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option B"",
        Value = ""B""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option C"",
        Value = ""C"",
        IsEnabled = false
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option D"",
        Value = ""D""
    }
};
";

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""Example2Options"" DefaultValue=""B"">
</BitChoiceGroup>";

    #endregion

    #region Example Code 3

    private readonly string example3CSharpCode = @"
public List<BitChoiceGroupOption> Example3Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Bar"",
        Value = ""Bar"",
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-bar-selected.png"",
        ImageAlt = ""alt for Bar image"",
        ImageSize = new Size(32, 32)
    },
    new BitChoiceGroupOption()
    {
        Text = ""Pie"",
        Value = ""Pie"",
        ImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};
";

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Options=""Example3Options"">
</BitChoiceGroup>";

    #endregion

    #region Example Code 4

    private readonly string example4CSharpCode = @"
public List<BitChoiceGroupOption> Example4Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Day"",
        Value = ""Day"",
        IconName = BitIconName.CalendarDay
    },
    new BitChoiceGroupOption()
    {
        Text = ""Week"",
        Value = ""Week"",
        IconName = BitIconName.CalendarWeek
    },
    new BitChoiceGroupOption()
    {
        Text = ""Month"",
        Value = ""Month"",
        IconName = BitIconName.Calendar,
        IsEnabled = false
    }
};
";

    private readonly string example4HtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Options=""Example4Options"">
</BitChoiceGroup>";

    #endregion

    #region Example Code 5

    private readonly string example5HtmlCode = @"
<BitChoiceGroup Options=""Example5Options"">
    <LabelFragment>
        Custom label <BitIconButton IconName= ""BitIconName.Filter""></BitIconButton>
    </LabelFragment>
</BitChoiceGroup>";

    private readonly string example5CSharpCode = @"
public List<BitChoiceGroupOption> Example5Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Option A"",
        Value = ""A""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option B"",
        Value = ""B""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option C"",
        Value = ""C""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option D"",
        Value = ""D""
    }
}; 
";

    #endregion

    #region Example Code 6

    private readonly string example6CSharpCode = @"
public List<BitChoiceGroupOption> Example6Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Text = ""Option A"",
        Value = ""A""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option B"",
        Value = ""B""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option C"",
        Value = ""C""
    },
    new BitChoiceGroupOption()
    {
        Text = ""Option D"",
        Value = ""D""
    }
};

public ChoiceGroupValidationModel ValidationModel = new();

public string SuccessMessage { get; set; } = string.Empty;

public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

private async Task HandleValidSubmit()
{
    SuccessMessage = ""Form Submitted Successfully!"";
    await Task.Delay(3000);
    SuccessMessage = string.Empty;
    StateHasChanged();
}

private void HandleInvalidSubmit()
{
    SuccessMessage = string.Empty;
}
";

    private readonly string example6HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Options = ""Example1Options"" @bind-Value=""ValidationModel.Value"">
            </BitChoiceGroup>
            <ValidationMessage For = ""@(() => ValidationModel.Value)"" />
        </div>
        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType = ""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

    #endregion

    #region Example Code 7

    private readonly string example7CSharpCode = @"
private List<BitChoiceGroupOption> example7Options;

/// <summary>
/// class constructure
/// </summary>
public BitChoiceGroupDemo()
{
    example7Options = new()
    {
        new BitChoiceGroupOption()
        {
            Text = ""Option A"",
            Value = ""A"",
            OnChange = () => OptionOnChange(""A"")
        },
        new BitChoiceGroupOption()
        {
            Text = ""Option B"",
            Value = ""B"",
            OnChange = () => OptionOnChange(""B"")
        },
        new BitChoiceGroupOption()
        {
            Text = ""Option C"",
            Value = ""C"",
            OnChange = () => OptionOnChange(""C"")
        },
        new BitChoiceGroupOption()
        {
            Text = ""Option D"",
            Value = ""D"",
            OnChange = () => OptionOnChange(""D"")
        }
    };
}

private void OptionOnChange(string value)
{
    OnChangeValue = value;
    StateHasChanged();
}
";

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""example7Options"">
</BitChoiceGroup>";

    #endregion

    #region Example Code 8

    private readonly string example8CSharpCode = @"
public static RenderFragment<BitChoiceGroupOption> LabelTemplateSample { get; set; }

private List<BitChoiceGroupOption> Example8Options { get; set; } = new()
{
    new BitChoiceGroupOption()
    {
        Value = ""A"",
        Text = ""Option A"",
        LabelTemplate = LabelTemplateSample
    },
    new BitChoiceGroupOption()
    {
        Value = ""B"",
        Text = ""Option B"",
        LabelTemplate = LabelTemplateSample
    },
    new BitChoiceGroupOption()
    {
        Value = ""C"",
        Text = ""Option C"",
        LabelTemplate = LabelTemplateSample
    },
    new BitChoiceGroupOption()
    {
        Value = ""D"",
        Text = ""Option D"",
        LabelTemplate = LabelTemplateSample
    }
};

protected override void OnInitialized()
{
    LabelTemplateSample = (option) => (
        @<div style=""border: 1px solid;"">
            <input type=""radio"" name=""input-name"" id=""input-id-@option.Value"">
            <label for=""input-id-@option.Value"">@option.Text</label>
        </div>
    );

    base.OnInitialized();
}
";

    private readonly string example8HtmlCode = @"
<BitChoiceGroup Label=""Pick one"" Options=""example8Options"">
</BitChoiceGroup>";

    #endregion

    private async Task HandleValidSubmit()
    {
        SuccessMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        SuccessMessage = string.Empty;
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        SuccessMessage = string.Empty;
    }

    private void OptionOnChange(string value)
    {
        OnChangeValue = value;
        StateHasChanged();
    }
}
