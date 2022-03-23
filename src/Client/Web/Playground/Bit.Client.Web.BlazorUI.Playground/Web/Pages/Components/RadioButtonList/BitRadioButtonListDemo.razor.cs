using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.RadioButtonList
{
    public partial class BitRadioButtonListDemo
    {
        private int ChosenGenderExample1;
        private int ChosenGenderExample2;
        private int ChosenGenderExample3;
        private int ChosenGenderExample4;
        private string SuccessMessage = string.Empty;
        private FormValidationModel FormValidationModel = new();

        private List<GenderModel> GenderOptionsExample1 = new()
        {
            new GenderModel { GenderId = 1, GenderText = "Female" },
            new GenderModel { GenderId = 2, GenderText = "Male" },
            new GenderModel { GenderId = 3, GenderText = "Other" },
            new GenderModel { GenderId = 4, GenderText = "Prefer not to say" },
        };

        private List<GenderModel> GenderOptionsExample2 = new()
        {
            new GenderModel { GenderId = 1, GenderText = "Female" },
            new GenderModel { GenderId = 2, GenderText = "Male" },
            new GenderModel { GenderId = 3, GenderText = "Other" },
            new GenderModel { GenderId = 4, GenderText = "Prefer not to say" },
        };

        private List<GenderModel> GenderOptionsExample3 = new()
        {
            new GenderModel { GenderId = 1, GenderText = "Female", ImageName = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png" },
            new GenderModel { GenderId = 2, GenderText = "Male", ImageName = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png" },
            new GenderModel { GenderId = 3, GenderText = "Other", ImageName = "https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png" },
            new GenderModel { GenderId = 4, GenderText = "Prefer not to say", ImageName = "https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png" },
        };

        private List<GenderModel> GenderOptionsExample4 = new()
        {
            new GenderModel { GenderId = 1, GenderText = "Female", IconName = BitIconName.People },
            new GenderModel { GenderId = 2, GenderText = "Male", IconName = BitIconName.People },
            new GenderModel { GenderId = 3, GenderText = "Other", IconName = BitIconName.PeopleBlock },
            new GenderModel { GenderId = 4, GenderText = "Prefer not to say", IconName = BitIconName.Emoji2 },
        };

        private List<GenderModel> GenderOptionsExample5 = new()
        {
            new GenderModel { GenderId = 1, GenderText = "Female", IconName = BitIconName.People },
            new GenderModel { GenderId = 2, GenderText = "Male", IconName = BitIconName.People },
            new GenderModel { GenderId = 3, GenderText = "Other", IconName = BitIconName.PeopleBlock },
            new GenderModel { GenderId = 4, GenderText = "Prefer not to say", IconName = BitIconName.Emoji2 },
        };

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

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "IconNameField",
                Type = "string",
                DefaultValue = "IconName",
                Description = "The name of the field from the model that will be the BitIconName.",
            },
            new ComponentParameter()
            {
                Name = "IconNameSelector",
                Type = "Expression<Func<TItem, BitIconName>>",
                DefaultValue = "",
                Description = "The field from the model that will be the BitIconName.",
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
                Name = "ImageSize",
                Type = "Size",
                DefaultValue = "",
                Description = "The width and height of the image in px for item field.",
            },
            new ComponentParameter()
            {
                Name = "ImageSrcSelector",
                Type = "Expression<Func<TItem, object>>",
                DefaultValue = "",
                Description = "The field from the model that will be the image src.",
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
                Name = "ImageAltSelector",
                Type = "Expression<Func<TItem, object>>",
                DefaultValue = "",
                Description = "The field from the model that will be the image alternate text.",
            },
            new ComponentParameter()
            {
                Name = "IsEnabledSelector",
                Type = "Expression<Func<TItem, bool>>",
                DefaultValue = "",
                Description = "The field from the model that will be enable item.",
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
                Name = "Items",
                Type = "IEnumerable<TItem>",
                DefaultValue = "",
                Description = "Sets the data source that populates the items of the list.",
            },
            new ComponentParameter()
            {
                Name = "OnClick",
                Type = "EventCallback<MouseEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the option clicked.",
            },
            new ComponentParameter()
            {
                Name = "OnChange",
                Type = "EventCallback<ChangeEventArgs>",
                DefaultValue = "",
                Description = "Callback for when the option has been changed.",
            },
            new ComponentParameter()
            {
                Name = "SelectedImageSrcSelector",
                Type = "Expression<Func<TItem, object>>",
                DefaultValue = "",
                Description = "The field from the model that will be the selected image src.",
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
                Name = "TextField",
                Type = "string",
                DefaultValue = "Text",
                Description = "The name of the field from the model that will be shown to the user.",
            },
            new ComponentParameter()
            {
                Name = "TextSelector",
                Type = "Expression<Func<TItem, object>>",
                DefaultValue = "",
                Description = "The field from the model that will be shown to the user.",
            },
            new ComponentParameter()
            {
                Name = "ValueChanged",
                Type = "EventCallback<TValue>",
                DefaultValue = "",
                Description = "Callback for when the input value changes.",
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
                Type = "Expression<Func<TItem, object>>",
                DefaultValue = "",
                Description = "The field from the model that will be the underlying value.",
            },
        };

        #region Example Code 1

        private readonly string example1HTMLCode = @"<div>Chosen gender: @(ChosenGenderExample1 == 0 ? ""no selection yet"" : ChosenGenderExample1.ToString())</div>
<BitRadioButtonList @bind-Value=""@ChosenGenderExample1""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    Items=""GenderOptionsExample1"" />";

        private readonly string example1CSharpCode = @"private int ChosenGenderExample1;
private List<GenderModel> GenderOptionsExample1 = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};";

        #endregion

        #region Example Code 2

        private readonly string example2HTMLCode = @"<BitRadioButtonList @bind-Value=""@ChosenGenderExample2""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    Items=""GenderOptionsExample2""
                    IsEnabled=""false"" />";

        private readonly string example2CSharpCode = @"private int ChosenGenderExample2;
private List<GenderModel> GenderOptionsExample2 = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"" },
};";

        #endregion

        #region Example Code 3

        private readonly string example3HTMLCode = @"<BitRadioButtonList @bind-Value=""@ChosenGenderExample3""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    Items=""GenderOptionsExample3""
                    ImageSrcField=""@nameof(GenderModel.ImageName)""
                    ImageAltField=""alt for image""
                    SelectedImageSrcField=""@nameof(GenderModel.ImageName)""
                    ImageSize=""new System.Drawing.Size( width: 32, height: 32)"" />";

        private readonly string example3CSharpCode = @"private int ChosenGenderExample3;
private List<GenderModel> GenderOptionsExample3 = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", ImageName = ""https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png"" },
    new GenderModel { GenderId = 2, GenderText = ""Male"", ImageName = ""https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png"" },
    new GenderModel { GenderId = 3, GenderText = ""Other"", ImageName = ""https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png"" },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", ImageName = ""https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png"" },
};";

        #endregion

        #region Example Code 4

        private readonly string example4HTMLCode = @"<BitRadioButtonList @bind-Value=""@ChosenGenderExample4""
                    TextField=""@nameof(GenderModel.GenderText)""
                    ValueField=""@nameof(GenderModel.GenderId)""
                    Items=""GenderOptionsExample4""
                    IconNameField=""@nameof(GenderModel.IconName)"" />";

        private readonly string example4CSharpCode = @"private int ChosenGenderExample4;
private List<GenderModel> GenderOptionsExample4 = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", IconName = BitIconName.People },
    new GenderModel { GenderId = 2, GenderText = ""Male"", IconName = BitIconName.People },
    new GenderModel { GenderId = 3, GenderText = ""Other"", IconName = BitIconName.PeopleBlock },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", IconName = BitIconName.Emoji2 },
};";

        #endregion

        #region Example Code 5

        private readonly string example5HTMLCode = @"@if (string.IsNullOrEmpty(SuccessMessage))
{
    <EditForm Model=""FormValidationModel"" OnValidSubmit=""HandleValidSubmit"" OnInvalidSubmit=""HandleInvalidSubmit"">
        <DataAnnotationsValidator />

        <div class=""validation-summary"">
            <ValidationSummary />
        </div>

        <div>
            <BitRadioButtonList @bind-Value=""@FormValidationModel.GenderId""
                TextField=""@nameof(GenderModel.GenderText)""
                ValueField=""@nameof(GenderModel.GenderId)""
                Items=""GenderOptionsExample5"" />

            <ValidationMessage For=""@(() => FormValidationModel.GenderId)"" />
        </div>

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

        private readonly string example5CSharpCode = @"public class FormValidationModel
{
    [Required]
    public int? GenderId { get; set; }
}
private string SuccessMessage = string.Empty;
private FormValidationModel FormValidationModel = new();
private List<GenderModel> GenderOptionsExample5 = new()
{
    new GenderModel { GenderId = 1, GenderText = ""Female"", IconName = BitIconName.People },
    new GenderModel { GenderId = 2, GenderText = ""Male"", IconName = BitIconName.People },
    new GenderModel { GenderId = 3, GenderText = ""Other"", IconName = BitIconName.PeopleBlock },
    new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", IconName = BitIconName.Emoji2 },
};

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
}";

        #endregion
    }
}
