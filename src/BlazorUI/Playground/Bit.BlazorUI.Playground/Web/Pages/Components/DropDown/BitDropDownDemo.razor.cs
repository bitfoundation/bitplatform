using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.DropDown;

public partial class BitDropDownDemo
{
    private string ControlledValue = "Apple";
    private List<string> ControlledValues = new List<string>() { "Apple", "Banana", "Grape" };
    private FormValidationDropDownModel formValidationDropDownModel = new();
    private string SuccessMessage = string.Empty;

    private async void HandleValidSubmit()
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

    private List<BitDropDownItem> GetCategoryDropdownItems()
    {
        List<BitDropDownItem> items = new();

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Fruits",
            Value = "f"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Vegetables",
            Value = "v"
        });

        return items;
    }

    private List<BitDropDownItem> GetProductDropdownItems()
    {
        List<BitDropDownItem> items = new();

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Fruits"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Apple",
            Value = "f-app"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Orange",
            Value = "f-ora",
            IsEnabled = false
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Banana",
            Value = "f-ban",
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Vegetables"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Broccoli",
            Value = "v-bro",
        });

        return items;
    }

    private List<BitDropDownItem> GetDropdownItems()
    {
        List<BitDropDownItem> items = new();

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Fruits"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Apple",
            Value = "f-app"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Orange",
            Value = "f-ora",
            IsEnabled = false
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Banana",
            Value = "f-ban",
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider,
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Vegetables"
        });

        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Broccoli",
            Value = "v-bro",
        });

        return items;
    }

    private List<BitDropDownItem> GetCustomDropdownItems()
    {
        List<BitDropDownItem> items = new();
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "Options",
            Value = "Header"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option a",
            Value = "A",
            Data = new DropDownItemData()
            {
                IconName = "Memo"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option b",
            Value = "B",
            Data = new DropDownItemData()
            {
                IconName = "Print"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option c",
            Value = "C",
            Data = new DropDownItemData()
            {
                IconName = "ShoppingCart"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option d",
            Value = "D",
            Data = new DropDownItemData()
            {
                IconName = "Train"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option e",
            Value = "E",
            Data = new DropDownItemData()
            {
                IconName = "Repair"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Divider
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Header,
            Text = "More options",
            Value = "Header2"
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option f",
            Value = "F",
            Data = new DropDownItemData()
            {
                IconName = "Running"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option g",
            Value = "G",
            Data = new DropDownItemData()
            {
                IconName = "EmojiNeutral"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option h",
            Value = "H",
            Data = new DropDownItemData()
            {
                IconName = "ChatInviteFriend"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option i",
            Value = "I",
            Data = new DropDownItemData()
            {
                IconName = "SecurityGroup"
            }
        });
        items.Add(new BitDropDownItem()
        {
            ItemType = BitDropDownItemType.Normal,
            Text = "Option j",
            Value = "J",
            Data = new DropDownItemData()
            {
                IconName = "AddGroup"
            }
        });

        return items;
    }

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "CaretDownFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Optional custom template for chevron icon.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValue",
            Type = "string",
            DefaultValue = "",
            Description = "Key that will be initially used to set selected item.",
        },
        new ComponentParameter()
        {
            Name = "DefaultValues",
            Type = "List<string>",
            DefaultValue = "",
            Description = "Keys that will be initially used to set selected items for multiSelect scenarios.",
        },
        new ComponentParameter()
        {
            Name = "IsMultiSelect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether multiple items are allowed to be selected.",
        },
        new ComponentParameter()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether or not this dropdown is open.",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "Requires the end user to select an item in the dropdown.",
        },
        new ComponentParameter()
        {
            Name = "Items",
            Type = "List<BitDropDownItem>",
            DefaultValue = "",
            Description = "A list of items to display in the dropdown.",
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<BitDropDownItem>",
            DefaultValue = "",
            Description = "Optional custom template for drop-down item.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string",
            DefaultValue = "",
            Description = "The title to show when the mouse is placed on the drop down.",
        },
        new ComponentParameter()
        {
            Name = "LabelFragment",
            Type = "RenderFragment",
            DefaultValue = "",
            Description = "Optional custom template for label.",
        },
        new ComponentParameter()
        {
            Name = "MultiSelectDelimiter",
            Type = "string",
            DefaultValue = "",
            Description = "When multiple items are selected, this still will be used to separate values in the dropdown title.",
        },
        new ComponentParameter()
        {
            Name = "NotifyOnReselect",
            Type = "bool",
            DefaultValue = "false",
            Description = "Optional preference to have OnSelectItem still be called when an already selected item is clicked in single select mode.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "",
            Description = "Callback for when the action button clicked.",
        },
        new ComponentParameter()
        {
            Name = "OnSelectItem",
            Type = "EventCallback<BitDropDownItem> ",
            DefaultValue = "",
            Description = "Callback for when an item is selected.",
        },
        new ComponentParameter()
        {
            Name = "Placeholder",
            Type = "string",
            DefaultValue = "",
            Description = "Input placeholder Text, Displayed until an option is selected.",
        },
        new ComponentParameter()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment<BitDropDown>",
            DefaultValue = "",
            Description = "Optional custom template for placeholder Text.",
        },
        new ComponentParameter()
        {
            Name = "Values",
            Type = "List<string>",
            DefaultValue = "",
            Description = "Keys of the selected items for multiSelect scenarios. If you provide this, you must maintain selection state by observing onChange events and passing a new value in when changed",
        },
        new ComponentParameter()
        {
            Name = "ValuesChanged",
            Type = "EventCallback<List<string>>",
            DefaultValue = "",
            Description = "Callback for when the values changed.",
        },
        new ComponentParameter()
        {
            Name = "TextTemplate",
            Type = "RenderFragment<BitDropDown>",
            DefaultValue = "",
            Description = "Optional custom template for selected option displayed in after selection.",
        },
        new ComponentParameter()
        {
            Name = "Visibility",
            Type = "BitComponentVisibility",
            LinkType = LinkType.Link,
            Href = "#component-visibility-enum",
            DefaultValue = "BitComponentVisibility.Visible",
            Description = "Whether the component is Visible,Hidden,Collapsed.",
        },
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new EnumParameter()
        {
            Id = "component-visibility-enum",
            Title = "BitComponentVisibility Enum",
            Description = "",
            EnumList = new List<EnumItem>()
            {
                new EnumItem()
                {
                    Name= "Visible",
                    Description="Show content of the component.",
                    Value="0",
                },
                new EnumItem()
                {
                    Name= "Hidden",
                    Description="Hide content of the component,though the space it takes on the page remains.",
                    Value="1",
                },
                new EnumItem()
                {
                    Name= "Collapsed",
                    Description="Hide content of the component,though the space it takes on the page gone.",
                    Value="2",
                }
            }
        }
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"<BitDropDown Label=""Basic Uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             Style=""width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>
<BitDropDown Label=""Disabled with defaultValue""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             IsEnabled=""false""
             DefaultValue=""v-bro""
             Style=""width: 290px; margin-bottom: 20px;"">
</BitDropDown>
<BitDropDown Label=""Multi-select uncontrolled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             IsMultiSelect=""true""
             Style=""width: 290px; margin-bottom: 20px;"">
</BitDropDown>";

    private readonly string example1CSharpCode = @"
private List<BitDropDownItem> GetDropdownItems()
{
    List<BitDropDownItem> items = new();

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Fruits""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Apple"",
        Value = ""f-app""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Orange"",
        Value = ""f-ora"",
        IsEnabled = false
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Banana"",
        Value = ""f-ban"",
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Divider,
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Vegetables""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Broccoli"",
        Value = ""v-bro"",
    });

    return items;
}";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"<BitDropDown Label=""Single-select Controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select an option""
             @bind-Value=""ControlledValue""
             Style=""width: 290px; margin: 20px 0 20px 0"">
</BitDropDown>";

    private readonly string example2CSharpCode = @"private string ControlledValue = ""Apple"";
private List<BitDropDownItem> GetDropdownItems()
{
    List<BitDropDownItem> items = new();

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Fruits""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Apple"",
        Value = ""f-app""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Orange"",
        Value = ""f-ora"",
        IsEnabled = false
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Banana"",
        Value = ""f-ban"",
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Divider,
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Vegetables""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Broccoli"",
        Value = ""v-bro"",
    });

    return items;
}";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"<BitDropDown Label=""Multi-select controlled""
             Items=""GetDropdownItems()""
             Placeholder=""Select options""
             @bind-Values=""ControlledValues""
             IsMultiSelect=""true""
             Style=""width:290px; margin:20px 0 20px 0"">
</BitDropDown>";

    private readonly string example3CSharpCode = @"private List<string> ControlledValues = new List<string>() { ""Apple"", ""Banana"", ""Grape"" };
private List<BitDropDownItem> GetDropdownItems()
{
    List<BitDropDownItem> items = new();

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Fruits""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Apple"",
        Value = ""f-app""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Orange"",
        Value = ""f-ora"",
        IsEnabled = false
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Banana"",
        Value = ""f-ban"",
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Divider,
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Vegetables""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Broccoli"",
        Value = ""v-bro"",
    });

    return items;
}";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"<BitDropDown Label=""Custom Controlled""
             Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             AriaLabel=""Custom dropdown""
             Style=""width:290px; margin:20px 0 20px 0"">
    <TextTemplate>
        <div>
            <i class=""bit-icon bit-icon--@((context.Items.Find(i => i.Value == context.Value).Data as DropDownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Items.Find(i => i.Value == context.Value).Data as DropDownItemData).IconName)""></i>
            <span>@context.Items.Find(i => i.Value == context.Value).Text</span>
        </div>
    </TextTemplate>
    <PlaceholderTemplate>
        <div>
            <i class=""bit-icon bit-icon--MessageFill"" aria-hidden=""true""></i>
            <span>@context.Placeholder</span>
        </div>
    </PlaceholderTemplate>
    <CaretDownFragment>
        <i class=""bit-icon bit-icon--CirclePlus""></i>
    </CaretDownFragment>
    <ItemTemplate>
        <div style=""display: flex; flex-flow: row nowrap; justify-content: flex-start; align-items: center;"">
            <i class=""bit-icon bit-icon--@((context.Data as DropDownItemData).IconName)""
               aria-hidden=""true""
               title=""@((context.Data as DropDownItemData).IconName)""></i>
            <span>@context.Text</span>
        </div>
    </ItemTemplate>
</BitDropDown>

<BitDropDown Items=""GetCustomDropdownItems()""
             Placeholder=""Select an option""
             Label=""Custom Label""
             AriaLabel=""Custom dropdown label ""
             Style=""width:290px"">
    <LabelFragment>
        <label>Custom label</label>
        <button type=""button"" title=""Info"" aria-label=""Info"" class=""custom-drp-lbl-ic"">
            <i class=""bit-icon bit-icon--Info""></i>
        </button>
    </LabelFragment>
</BitDropDown>";

    private readonly string example4CSharpCode = @"
private List<BitDropDownItem> GetCustomDropdownItems()
{
    List<BitDropDownItem> items = new();
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Options"",
        Value = ""Header""
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option a"",
        Value = ""A"",
        Data = new DropDownItemData()
        {
            IconName = ""Memo""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option b"",
        Value = ""B"",
        Data = new DropDownItemData()
        {
            IconName = ""Print""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option c"",
        Value = ""C"",
        Data = new DropDownItemData()
        {
            IconName = ""ShoppingCart""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option d"",
        Value = ""D"",
        Data = new DropDownItemData()
        {
            IconName = ""Train""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option e"",
        Value = ""E"",
        Data = new DropDownItemData()
        {
            IconName = ""Repair""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Divider
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""More options"",
        Value = ""Header2""
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option f"",
        Value = ""F"",
        Data = new DropDownItemData()
        {
            IconName = ""Running""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option g"",
        Value = ""G"",
        Data = new DropDownItemData()
        {
            IconName = ""EmojiNeutral""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option h"",
        Value = ""H"",
        Data = new DropDownItemData()
        {
            IconName = ""ChatInviteFriend""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option i"",
        Value = ""I"",
        Data = new DropDownItemData()
        {
            IconName = ""SecurityGroup""
        }
    });
    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Option j"",
        Value = ""J"",
        Data = new DropDownItemData()
        {
            IconName = ""AddGroup""
        }
    });

    return items;
}";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""formValidationDropDownModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitDropDown Label=""Select category""
                        Items=""GetCategoryDropdownItems()""
                        IsMultiSelect=""false""
                        @bind-Value=""formValidationDropDownModel.Category""
                        Placeholder=""Select an option""
                        Style=""width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropDownModel.Category)"" />
        </div>

        <div>
            <BitDropDown Label=""Select two ptoducts""
                        Items=""GetProductDropdownItems()""
                        IsMultiSelect=""true""
                        @bind-Values=""formValidationDropDownModel.Products""
                        Placeholder=""Select an option""
                        Style=""width: 290px; margin: 20px 0 20px 0"" />

            <ValidationMessage For=""@(() => formValidationDropDownModel.Products)"" />
        </div>

        <br />

        <BitButton ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}";

    private readonly string example5CSharpCode = @"
public class FormValidationDropDownModel
{
    [MaxLength(2, ErrorMessage = ""The property {0} doesn't have more than {1} elements"")]
    [MinLength(1, ErrorMessage = ""The property {0} doesn't have less than {1} elements"")]
    public List<string> Products { get; set; } = new();

    [Required]
    public string Category { get; set; }
}

private FormValidationDropDownModel formValidationDropDownModel = new();
private string SuccessMessage = string.Empty;

private async void HandleValidSubmit()
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

private List<BitDropDownItem> GetCategoryDropdownItems()
{
    List<BitDropDownItem> items = new();

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Fruits"",
        Value = ""f""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Vegetables"",
        Value = ""v""
    });

    return items;
}

private List<BitDropDownItem> GetProductDropdownItems()
{
    List<BitDropDownItem> items = new();

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Fruits""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Apple"",
        Value = ""f-app""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Orange"",
        Value = ""f-ora"",
        IsEnabled = false
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Banana"",
        Value = ""f-ban"",
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Divider,
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Header,
        Text = ""Vegetables""
    });

    items.Add(new BitDropDownItem()
    {
        ItemType = BitDropDownItemType.Normal,
        Text = ""Broccoli"",
        Value = ""v-bro"",
    });

    return items;
}";

    #endregion
}
