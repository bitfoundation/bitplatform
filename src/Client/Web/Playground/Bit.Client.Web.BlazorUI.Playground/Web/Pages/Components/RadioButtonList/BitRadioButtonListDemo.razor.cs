using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.RadioButtonList
{
    public partial class BitRadioButtonListDemo
    {
        int ChosenGenderExample1 { get; set; }
        int ChosenGenderExample2 { get; set; }
        int ChosenGenderExample3 { get; set; }
        int ChosenGenderExample4 { get; set; }


        List<GenderModel> GenderOptionsExample1 { get; set; } = new List<GenderModel>
        {
        new GenderModel { GenderId = 1, GenderText = "Female",IconName=null },
        new GenderModel { GenderId = 2, GenderText = "Male",IconName=null },
        new GenderModel { GenderId = 3, GenderText = "Other",IconName=null },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say",IconName=null },
        };

        List<GenderModel> GenderOptionsExample3 { get; set; } = new List<GenderModel>
        {
        new GenderModel { GenderId = 1, GenderText = "Female",ImageName="https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png",IconName=null },
        new GenderModel { GenderId = 2, GenderText = "Male",ImageName="https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png",IconName=null },
        new GenderModel { GenderId = 3, GenderText = "Other",ImageName="https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png",IconName=null },
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say",ImageName="https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png",IconName=null },
        };

        List<GenderModel> GenderOptionsExample4 { get; set; } = new List<GenderModel>
        {
        new GenderModel { GenderId = 1, GenderText = "Female",IconName=BitIconName.People },
        new GenderModel { GenderId = 2, GenderText = "Male" ,IconName=BitIconName.People},
        new GenderModel { GenderId = 3, GenderText = "Other" ,IconName=BitIconName.PeopleBlock},
        new GenderModel { GenderId = 4, GenderText = "Prefer not to say" ,IconName=BitIconName.Emoji2},
        };

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

        private readonly string example1CSharpCode = @"

int ChosenGenderExample1 { get; set; }
List<GenderModel> GenderOptionsExample1 { get; set; } = new List<GenderModel>
    {
        new GenderModel { GenderId = 1, GenderText = ""Female"",IconName=null },
        new GenderModel { GenderId = 2, GenderText = ""Male"",IconName=null },
        new GenderModel { GenderId = 3, GenderText = ""Other"",IconName=null },
        new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"",IconName=null },
    }; ";

        private readonly string example2CSharpCode = @"

int ChosenGenderExample2 { get; set; }
List<GenderModel> GenderOptionsExample1 { get; set; } = new List<GenderModel>
    {
        new GenderModel { GenderId = 1, GenderText = ""Female"",IconName=null },
        new GenderModel { GenderId = 2, GenderText = ""Male"",IconName=null },
        new GenderModel { GenderId = 3, GenderText = ""Other"",IconName=null },
        new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"",IconName=null },
    }; ";

        private readonly string example3CSharpCode = @"

int ChosenGenderExample3 { get; set; }
List<GenderModel> GenderOptionsExample3 { get; set; } = new List<GenderModel>
    {
        new GenderModel { GenderId = 1, GenderText = ""Female"",ImageName=""https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png"",IconName=null },
        new GenderModel { GenderId = 2, GenderText = ""Male"",ImageName=""https://upload.wikimedia.org/wikipedia/commons/thumb/a/a3/Map_icons_by_Scott_de_Jonge_-_male.svg/1024px-Map_icons_by_Scott_de_Jonge_-_male.svg.png"",IconName=null },
        new GenderModel { GenderId = 3, GenderText = ""Other"",ImageName=""https://cdn1.iconfinder.com/data/icons/robots-avatars-set/354/Robot_bot___robot_robo_bot_artificial_intelligence-512.png"",IconName=null },
        new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"", ImageName = ""https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png"", IconName = null },
    }; ";

        private readonly string example4CSharpCode = @"

int ChosenGenderExample4 { get; set; }
List<GenderModel> GenderOptionsExample4 { get; set; } = new List<GenderModel>
    {
        new GenderModel { GenderId = 1, GenderText = ""Female"",IconName=BitIconName.People },
        new GenderModel { GenderId = 2, GenderText = ""Male"",IconName=BitIconName.People },
        new GenderModel { GenderId = 3, GenderText = ""Other"",IconName=BitIconName.PeopleBlock },
        new GenderModel { GenderId = 4, GenderText = ""Prefer not to say"",IconName=BitIconName.Emoji2 },
    }; ";

        private readonly string example1HTMLCode = @"<div>Chosen gender: @( ChosenGenderExample1 == 0 ? ""no selection yet"" : ChosenGenderExample1.ToString() )</div>
<BitRadioButtonList @bind-Value=""@ChosenGenderExample1""
                                    TextField=""@nameof(GenderModel.GenderText)""
                                    ValueField=""@nameof(GenderModel.GenderId)""
                                    Items=""GenderOptionsExample1"" />";

        private readonly string example2HTMLCode = @"<BitRadioButtonList @bind-Value=""@ChosenGenderExample2""
                                    TextField=""@nameof(GenderModel.GenderText)""
                                    ValueField=""@nameof(GenderModel.GenderId)""
                                    Items=""GenderOptionsExample1""
                                    IsEnabled=""false""/>";

        private readonly string example3HTMLCode = @"<BitRadioButtonList @bind-Value=""@ChosenGenderExample3""
                                        TextField=""@nameof(GenderModel.GenderText)""
                                        ValueField=""@nameof(GenderModel.GenderId)""
                                        Items=""GenderOptionsExample3""
                                        ImageSrcField=""@nameof(GenderModel.ImageName)""
                                        ImageAltField=""alt for image""
                                        SelectedImageSrcField=""@nameof(GenderModel.ImageName)""
                                        ImageSize=""new System.Drawing.Size( width: 32, height: 32)"" />";

        private readonly string example4HTMLCode = @"
 <BitRadioButtonList @bind-Value=""@ChosenGenderExample4""
                                TextField=""@nameof(GenderModel.GenderText)""
                                ValueField=""@nameof(GenderModel.GenderId)""
                                Items=""GenderOptionsExample4""
                                IconNameField=""@nameof(GenderModel.IconName)"" />";
    }
    public class GenderModel
    {
        public int GenderId { get; set; }
        public string GenderText { get; set; }
        public string ImageName { get; set; }
        public BitIconName? IconName { get; set; }
    }

}
