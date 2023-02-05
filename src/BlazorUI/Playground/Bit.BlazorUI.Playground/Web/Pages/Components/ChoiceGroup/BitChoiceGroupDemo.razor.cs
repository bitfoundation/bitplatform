using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup;

public partial class BitChoiceGroupDemo
{
    private string ChoiceGroupWithOptionTemplateValue = "Day";
    private string ChoiceGroupWithOptionLabelTemplateValue = "Day";
    private string ChoiceGroupOneWayValue = "A";
    private string ChoiceGroupTwoWayValue = "A";
    private string ChoiceGroupLayoutFlowWithOptionTemplateValue = "Day";
    private string ChoiceGroupRtlLayoutFlowWithOptionTemplateValue = "Day";
    public ChoiceGroupValidationModel ValidationModel = new();
    public string SuccessMessage;

    private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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
    private List<BitChoiceGroupOption> ChoiceGroupWithDisabledOption = new()
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
    private List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
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
    private List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
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
    private List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
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
            IconName = BitIconName.Calendar
        }
    };

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter
        {
            Name = "AriaLabelledBy",
            Type = "string?",
            Description = "ID of an element to use as the aria label for this ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "DefaultValue",
            Type = "string?",
            Description = "Default selected Value for ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, an option must be selected in the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "IsRtl",
            Type = "bool",
            DefaultValue = "false",
            Description = "Change direction to RTL."
        },
        new ComponentParameter
        {
            Name = "Label",
            Type = "string?",
            Description = "Descriptive label for the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the label for the ChoiceGroup."
        },
        new ComponentParameter
        {
            Name = "LayoutFlow",
            Type = "BitLayoutFlow?",
            Description = "You can define the ChoiceGroup in Horizontal or Vertical mode."
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
            Name = "Options",
            Type = "List<BitChoiceGroupOption>",
            DefaultValue = "new List<BitChoiceGroupOption>()",
            Description = "List of options, each of which is a selection in the ChoiceGroup.",
            LinkType = LinkType.Link,
            Href = "#choice-group-option"
        },
        new ComponentParameter
        {
            Name = "OptionTemplate",
            Type = "RenderFragment<BitChoiceGroupOption>?",
            Description = "Used to customize the Option for the ChoiceGroup."
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<BitChoiceGroupOption>",
            Description = "Callback that is called when the ChoiceGroup value has changed.",
        }
    };
    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new ComponentSubParameter()
        {
            Id = "choice-group-option",
            Title = "BitChoiceGroupOption",
            Parameters = new List<ComponentParameter>()
            {
               new ComponentParameter()
               {
                   Name = "AriaLabel",
                   Type = "string?",
                   Description = "AriaLabel attribute for the GroupOption Option input.",
               },
               new ComponentParameter()
               {
                   Name = "IsEnabled",
                   Type = "bool",
                   DefaultValue = "true",
                   Description = "Whether or not the GroupOption Option is enabled.",
               },
               new ComponentParameter()
               {
                   Name = "IconName",
                   Type = "BitIconName?",
                   Description = "The icon to show as Option content.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSrc",
                   Type = "string?",
                   Description = "The image address to show as Option content.",
               },
               new ComponentParameter()
               {
                   Name = "ImageAlt",
                   Type = "string?",
                   Description = "Provides alternative information for the Option image.",
               },
               new ComponentParameter()
               {
                   Name = "ImageSize",
                   Type = "Size?",
                   Description = "Provides Height and Width for the Option image.",
               },
               new ComponentParameter()
               {
                   Name = "Id",
                   Type = "string?",
                   Description = "Set attribute of Id for the GroupOption Option input.",
               },
               new ComponentParameter()
               {
                   Name = "LabelId",
                   Type = "string?",
                   Description = "Set attribute of Id for the GroupOption Option label.",
               },
               new ComponentParameter()
               {
                   Name = "SelectedImageSrc",
                   Type = "string?",
                   Description = "Provides a new image for the selected Option in the Image-GroupOption.",
               },
               new ComponentParameter()
               {
                   Name = "Text",
                   Type = "string?",
                   Description = "Text to show as content of GroupOption Option.",
               },
               new ComponentParameter()
               {
                   Name = "Value",
                   Type = "string?",
                   Description = "This value is returned when GroupOption Option is Clicked.",
               }
            }
        }
    };

    #region Example Code 1

    private readonly string example1HtmlCode = @"
 <BitChoiceGroup Label=""Pick one"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" />
";

    private readonly string example1CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

    private readonly string example2HtmlCode = @"
<BitChoiceGroup Label=""Disabled ChoiceGroup"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" />

<BitChoiceGroup Label=""ChoiceGroup with Disabled Option"" Options=""ChoiceGroupWithDisabledOption"" DefaultValue=""A"" />
";

    private readonly string example2CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

private List<BitChoiceGroupOption> ChoiceGroupWithDisabledOption = new()
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

    #endregion

    #region Example Code 3

    private readonly string example3HtmlCode = @"
<BitChoiceGroup Label=""Pick one image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" />
";

    private readonly string example3CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
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
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};
";

    #endregion

    #region Example Code 4

    private readonly string example4HtmlCode = @"
<BitChoiceGroup Label=""Pick one icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" />
";

    private readonly string example4CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
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

    #endregion

    #region Example Code 5

    private readonly string example5HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }
</style>

<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>
";

    private readonly string example5CSharpCode = @"
private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

    private readonly string example6HtmlCode = @"
<style>
    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitChoiceGroup Label=""Pick one"" Options=""ChoiceGroupWithOptionTemplate"" @bind-Value=""ChoiceGroupWithOptionTemplateValue"" DefaultValue=""Day"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Label Template"" Options=""ChoiceGroupWithOptionTemplate"" @bind-Value=""ChoiceGroupWithOptionLabelTemplateValue"" DefaultValue=""Day"">
    <OptionLabelTemplate Context=""option"">
        <div style=""margin-left: 27px;"" class=""custom-option @(ChoiceGroupWithOptionLabelTemplateValue == option.Value ? ""selected-option"" : """")"">
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionLabelTemplate>
</BitChoiceGroup>
";

    private readonly string example6CSharpCode = @"
private string ChoiceGroupWithOptionTemplateValue;

private string ChoiceGroupWithOptionLabelTemplateValue;

private List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
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
        IconName = BitIconName.Calendar
    }
};
";

    #endregion

    #region Example Code 7

    private readonly string example7HtmlCode = @"
<BitChoiceGroup Label=""One-way"" Options=""ChoiceGroupBasicOption"" Value=""@ChoiceGroupOneWayValue"" DefaultValue=""A"" />

<BitChoiceGroup Label=""Two-way"" Options=""ChoiceGroupBasicOption"" @bind-Value=""ChoiceGroupTwoWayValue"" DefaultValue=""A"" />
";

    private readonly string example7CSharpCode = @"
private string ChoiceGroupOneWayValue;
private string ChoiceGroupTwoWayValue;

private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

    #region Example Code 8

    private readonly string example8HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }

    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitChoiceGroup Label=""Basic"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Disabled"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Label=""Icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" />

<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup Label=""Option Template""
                Options=""ChoiceGroupWithOptionTemplate""
                @bind-Value=""ChoiceGroupLayoutFlowWithOptionTemplateValue""
                DefaultValue=""Day""
                LayoutFlow=""BitLayoutFlow.Horizontal"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>
";

    private readonly string example8CSharpCode = @"
private string ChoiceGroupLayoutFlowWithOptionTemplateValue;

private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

private List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
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
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
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

private List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
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
        IconName = BitIconName.Calendar
    }
};
";

    #endregion

    #region Example Code 9

    private readonly string example9HtmlCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: red;
    }

    .custom-option {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .option-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .option-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-option {
            color: #C66;

            .option-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitChoiceGroup Label=""Basic"" Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" IsRtl=""true"" />

<BitChoiceGroup Label=""Disabled"" Options=""ChoiceGroupBasicOption"" IsEnabled=""false"" DefaultValue=""A"" IsRtl=""true"" />

<BitChoiceGroup Label=""Image"" Options=""ChoiceGroupWithImage"" DefaultValue=""Bar"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />

<BitChoiceGroup Label=""Icon"" Options=""ChoiceGroupWithIcon"" DefaultValue=""Day"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"" />

<BitChoiceGroup Options=""ChoiceGroupBasicOption"" DefaultValue=""A"" LayoutFlow=""BitLayoutFlow.Horizontal"" IsRtl=""true"">
    <LabelTemplate>
        <div class=""custom-label"">
            Label Template <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitChoiceGroup>

<BitChoiceGroup @bind-Value=""ChoiceGroupRtlWithOptionTemplateValue""
                Label=""Option Template""
                Options=""ChoiceGroupWithOptionTemplate""
                DefaultValue=""Day""
                LayoutFlow=""BitLayoutFlow.Horizontal""
                IsRtl=""true"">
    <OptionTemplate Context=""option"">
        <div class=""custom-option @(ChoiceGroupLayoutFlowWithOptionTemplateValue == option.Value ? ""selected-option"" : """")"">
            <div class=""option-pointer""></div>
            <BitIcon IconName=""@option.IconName.Value"" />
            <span>@option.Text</span>
        </div>
    </OptionTemplate>
</BitChoiceGroup>
";

    private readonly string example9CSharpCode = @"
private string ChoiceGroupRtlWithOptionTemplateValue;

private List<BitChoiceGroupOption> ChoiceGroupBasicOption = new()
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

private List<BitChoiceGroupOption> ChoiceGroupWithImage = new()
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
        ImageSrc= ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-unselected.png"",
        SelectedImageSrc = ""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/choicegroup-pie-selected.png"",
        ImageAlt = ""alt for Pie image"",
        ImageSize = new Size(32, 32)
    }
};

private List<BitChoiceGroupOption> ChoiceGroupWithIcon = new()
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

private List<BitChoiceGroupOption> ChoiceGroupWithOptionTemplate = new()
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
        IconName = BitIconName.Calendar
    }
};
";

    #endregion

    #region Example Code 10

    private readonly string example10HtmlCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""@ValidationModel"" OnValidSubmit=""@HandleValidSubmit"" OnInvalidSubmit=""@HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitChoiceGroup Options=""ChoiceGroupBasicOption"" @bind-Value=""ValidationModel.Value"" />
            <ValidationMessage For=""@(() => ValidationModel.Value)"" />
        </div>
        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">Submit</BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}
";

    private readonly string example10CSharpCode = @"
public class ChoiceGroupValidationModel
{
    [Required(ErrorMessage = ""Pick one"")]
    public string Value { get; set; }
}

public ChoiceGroupValidationModel ValidationModel = new();
public string SuccessMessage;

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
}
