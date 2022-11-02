using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.RadioButtonList;

public partial class BitRadioButtonListDemo
{
    private int example1FieldApproacheValue;
    private int example1SelectorApproacheValue;
    private int example2DisableItemsValue;
    private int example2DisableItemValue;
    private int example3Value;
    private int example4Value;
    private int example5Value;
    private int example6ItemTemplateValue;
    private int example6ItemLabelTemplateValue;
    private string SuccessMessage = string.Empty;
    private FormValidationModel FormValidationModel = new();

    private List<GenderModel> BasicGenderItems = new()
    {
        new GenderModel { GenderId = 1, GenderText = "Female" },
        new GenderModel { GenderId = 2, GenderText = "Male" },
        new GenderModel { GenderId = 3, GenderText = "Other" },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say" },
    };
    private List<GenderModel> DisabledGenderItems = new()
    {
        new GenderModel { GenderId = 1, GenderText = "Female" },
        new GenderModel { GenderId = 2, GenderText = "Male" },
        new GenderModel { GenderId = 3, GenderText = "Other" },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say", IsEnabled = false },
    };
    private List<GenderModel> ImageGenderItems = new()
    {
        new GenderModel { GenderId = 1, GenderText = "Female", ImageName = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png" },
        new GenderModel { GenderId = 2, GenderText = "Male", ImageName = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png" },
        new GenderModel { GenderId = 3, GenderText = "Other", ImageName = "https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png" },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say", ImageName = "https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png" },
    };
    private List<GenderModel> IconGenderItems = new()
    {
        new GenderModel { GenderId = 1, GenderText = "Female", IconName = BitIconName.People },
        new GenderModel { GenderId = 2, GenderText = "Male", IconName = BitIconName.People },
        new GenderModel { GenderId = 3, GenderText = "Other", IconName = BitIconName.PeopleBlock },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say", IconName = BitIconName.Emoji2 },
    };

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

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new ComponentParameter()
        {
            Name = "AriaLabelledBy",
            Type = "string",
            DefaultValue = "string.Empty",
            Description = "ID of an element to use as the aria label for this RadioButtonList.",
        },
        new ComponentParameter()
        {
            Name = "Items",
            Type = "IEnumerable<TItem>",
            DefaultValue = "new List<TItem>()",
            Description = "Sets the data source that populates the items of the list.",
        },
        new ComponentParameter()
        {
            Name = "ItemTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize the label for the Item content.",
        },
        new ComponentParameter()
        {
            Name = "ItemLabelTemplate",
            Type = "RenderFragment<TItem>?",
            Description = "Used to customize the label for the Item Label content.",
        },
        new ComponentParameter()
        {
            Name = "IsRequired",
            Type = "bool",
            Description = "If true, an option must be selected in the RadioButtonList.",
        },
        new ComponentParameter()
        {
            Name = "IsEnabledField",
            Type = "string",
            DefaultValue = "IsEnabled",
            Description = "The name of the field from the model that will be enable item.",
        },
        new ComponentParameter()
        {
            Name = "IsEnabledSelector",
            Type = "Expression<Func<TItem, bool>>?",
            Description = "The field from the model that will be enable item.",
        },
        new ComponentParameter()
        {
            Name = "IconNameField",
            Type = "string",
            DefaultValue = "IconName",
            Description = "The name of the field from the model that will be the BitIconName.",
        },
        new ComponentParameter()
        {
            Name = "ImageSrcField",
            Type = "string",
            DefaultValue = "ImageSrc",
            Description = "The name of the field from the model that will be the image src.",
        },
        new ComponentParameter()
        {
            Name = "ImageAltField",
            Type = "string",
            DefaultValue = "ImageAlt",
            Description = "The name of the field from the model that will be the image alternate text.",
        },
        new ComponentParameter()
        {
            Name = "IconNameSelector",
            Type = "Expression<Func<TItem, BitIconName>>?",
            Description = "The field from the model that will be the BitIconName.",
        },
        new ComponentParameter()
        {
            Name = "ImageSrcSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "The field from the model that will be the image src.",
        },
        new ComponentParameter()
        {
            Name = "ImageAltSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "The field from the model that will be the image alternate text.",
        },
        new ComponentParameter()
        {
            Name = "ImageSize",
            Type = "Size?",
            Description = "The width and height of the image in px for item field.",
        },
        new ComponentParameter()
        {
            Name = "Label",
            Type = "string?",
            Description = "Descriptive label for the radio button list.",
        },
        new ComponentParameter()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            Description = "Used to customize the label for the radio button list.",
        },
        new ComponentParameter()
        {
            Name = "Name",
            Type = "string",
            DefaultValue = "a guid",
            Description = "Name of RadioButtonList, this name is used to group each item into the same logical RadioButtonList.",
        },
        new ComponentParameter()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback for when the option clicked.",
        },
        new ComponentParameter()
        {
            Name = "OnChange",
            Type = "EventCallback<ChangeEventArgs>",
            Description = "Callback for when the option has been changed.",
        },
        new ComponentParameter()
        {
            Name = "SelectedImageSrcField",
            Type = "string",
            DefaultValue = "SelectedImageSrc",
            Description = "The name of the field from the model that will be the selected image src.",
        },
        new ComponentParameter()
        {
            Name = "SelectedImageSrcSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "The field from the model that will be the selected image src.",
        },
        new ComponentParameter()
        {
            Name = "TextField",
            Type = "string",
            DefaultValue = "Text",
            Description = "The name of the field from the model that will be shown to the user.",
        },
        new ComponentParameter()
        {
            Name = "TextSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "The field from the model that will be shown to the user.",
        },
        new ComponentParameter()
        {
            Name = "ValueField",
            Type = "string",
            DefaultValue = "Value",
            Description = "The name of the field from the model that will be the underlying value.",
        },
        new ComponentParameter()
        {
            Name = "ValueSelector",
            Type = "Expression<Func<TItem, object>>?",
            Description = "The field from the model that will be the underlying value.",
        },
    };

    #region Example Code 1

    private readonly string example1HTMLCode = @"
<BitRadioButtonList @bind-Value=""@example1FieldApproacheValue""
                    Label=""Field parameter""
                    Items=""BasicGenderItems""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)"" />

<BitRadioButtonList @bind-Value=""@example1SelectorApproacheValue""
                    Label=""Selector parameter""
                    Items=""BasicGenderItems""
                    TextSelector=""item => item.GenderText""
                    ValueSelector=""item => item.GenderId"" />
";

    private readonly string example1CSharpCode = @"
private int example1FieldApproacheValue;
private int example1SelectorApproacheValue;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> BasicGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};
";

    #endregion

    #region Example Code 2

    private readonly string example2HTMLCode = @"
<BitRadioButtonList @bind-Value=""@example2DisableItemsValue""
                    Label=""All item disabled""
                    Items=""BasicGenderItems""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    IsEnabled=""false"" />

<BitRadioButtonList @bind-Value=""@example2DisableItemValue""
                    Label=""Specific item disabled""
                    Items=""DisabledGenderItems""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    IsEnabledField=""@nameof(GenderModel.IsEnabled)"" />
";

    private readonly string example2CSharpCode = @"
private int example2DisableItemsValue;
private int example2DisableItemValue;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> BasicGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};

private List<GenderModel> DisabledGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", IsEnabled = false },
};
";

    #endregion

    #region Example Code 3

    private readonly string example3HTMLCode = @"
<BitRadioButtonList @bind-Value=""@example3Value""
                    Items=""ImageGenderItems""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    ImageSrcField=""@nameof(GenderModel.ImageName)""
                    ImageAltField=""alt for image""
                    SelectedImageSrcField=""@nameof(GenderModel.ImageName)""
                    ImageSize=""new System.Drawing.Size( width: 32, height: 32)"" />
";

    private readonly string example3CSharpCode = @"
private int example3Value;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> ImageGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", ImageName = ""https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"", ImageName = ""https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"", ImageName = ""https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", ImageName = ""https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png"" },
};
";

    #endregion

    #region Example Code 4

    private readonly string example4HTMLCode = @"
<BitRadioButtonList @bind-Value=""@example4Value""
                    Items=""IconGenderItems""
                    TextSelector=""item => item.GenderText""
                    ValueSelector=""item => item.GenderId""
                    IconNameSelector=""item => (BitIconName)item.IconName"" />
";

    private readonly string example4CSharpCode = @"
private int example4Value;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> IconGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", IconName = BitIconName.People },
    new GenderModel { GenderId = 2, GenderText = ""Male"", IconName = BitIconName.People },
    new GenderModel { GenderId = 3, GenderText = ""Other"", IconName = BitIconName.PeopleBlock },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", IconName = BitIconName.Emoji2 },
};
";

    #endregion

    #region Example Code 5

    private readonly string example5HTMLCode = @"
<style>
    .custom-label {
        font-weight: bold;
        color: $Red20;
    }
</style>

<BitRadioButtonList @bind-Value=""@example5Value""
                    Items=""BasicGenderItems""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)"">
    <LabelTemplate>
        <div class=""custom-label"">
            Custom label <BitIcon IconName=""BitIconName.Filter"" />
        </div>
    </LabelTemplate>
</BitRadioButtonList>
";

    private readonly string example5CSharpCode = @"
private int example5Value;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> BasicGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};
";

    #endregion

    #region Example Code 6

    private readonly string example6HTMLCode = @"
<style>
    .custom-item {
        display: flex;
        align-items: center;
        gap: rem(10px);
        cursor: pointer;

        .radio-pointer {
            width: rem(20px);
            height: rem(20px);
            border: 1px solid;
            border-radius: rem(10px);
        }

        &:hover {
            .radio-pointer {
                border-top: rem(5px) solid #C66;
                border-bottom: rem(5px) solid #6C6;
                border-left: rem(5px) solid #66C;
                border-right: rem(5px) solid #CC6;
            }
        }

        &.selected-item {
            color: #C66;

            .radio-pointer {
                border-top: rem(10px) solid #C66;
                border-bottom: rem(10px) solid #6C6;
                border-left: rem(10px) solid #66C;
                border-right: rem(10px) solid #CC6;
            }
        }
    }
</style>

<BitRadioButtonList @bind-Value=""@example6ItemTemplateValue""
                    Label=""Item Template""
                    Items=""IconGenderItems""
                    ValueField=""@nameof(GenderModel.GenderId)"">
    <ItemTemplate Context=""item"">
        <div class=""custom-item @(example6ItemTemplateValue == item.GenderId ? ""selected-item"" : """")"">
            <div class=""radio-pointer""></div>
            <BitIcon IconName=""@((BitIconName)item.IconName)"" />
            <span>@item.GenderText</span>
        </div>
    </ItemTemplate>
</BitRadioButtonList>

<BitRadioButtonList @bind-Value=""@example6ItemLabelTemplateValue""
                    Label=""Item Label Template""
                    Items=""IconGenderItems""
                    ValueField=""@nameof(GenderModel.GenderId)"">
    <ItemLabelTemplate Context=""item"">
        <div style=""margin-left: 27px;"" class=""custom-item @(example6ItemLabelTemplateValue == item.GenderId ? ""selected-item"" : """")"">
            <BitIcon IconName=""@((BitIconName)item.IconName)"" />
            <span>@item.GenderText</span>
        </div>
    </ItemLabelTemplate>
</BitRadioButtonList>
";

    private readonly string example6CSharpCode = @"
private int example6ItemTemplateValue;
private int example6ItemLabelTemplateValue;

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> IconGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", IconName = BitIconName.People },
    new GenderModel { GenderId = 2, GenderText = ""Male"", IconName = BitIconName.People },
    new GenderModel { GenderId = 3, GenderText = ""Other"", IconName = BitIconName.PeopleBlock },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", IconName = BitIconName.Emoji2 },
};
";

    #endregion

    #region Example Code 7

    private readonly string example7HTMLCode = @"
@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""FormValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />
        <div class=""validation-summary"">
            <ValidationSummary />
        </div>
        <div>
            <BitRadioButtonList @bind-Value=""@FormValidationModel.GenderId""
                                Items=""BasicGenderItems""
                                TextField=""@nameof(GenderModel.GenderText)""
                                ValueField=""@nameof(GenderModel.GenderId)"" />
            <ValidationMessage For=""@(() => FormValidationModel.GenderId)"" />
        </div>
        <BitButton Style=""margin-top: 10px;"" ButtonType=""BitButtonType.Submit"">
            Submit
        </BitButton>
    </EditForm>
}
else
{
    <BitMessageBar MessageBarType=""BitMessageBarType.Success"" IsMultiline=""false"">
        @SuccessMessage
    </BitMessageBar>
}
";

    private readonly string example7CSharpCode = @"
public class FormValidationModel
{
    [Required]
    public int? GenderId { get; set; }
}

private string SuccessMessage = string.Empty;
private FormValidationModel FormValidationModel = new();

public class GenderModel
{
    public int GenderId { get; set; }
    public string GenderText { get; set; }
    public string ImageName { get; set; }
    public BitIconName? IconName { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<GenderModel> BasicGenderItems = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};

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
}
