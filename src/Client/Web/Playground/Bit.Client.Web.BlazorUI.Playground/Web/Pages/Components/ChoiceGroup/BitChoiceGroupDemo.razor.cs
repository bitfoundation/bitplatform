using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Components.ChoiceGroup;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ChoiceGroup
{
    public partial class BitChoiceGroupDemo
    {
        public List<BitChoiceGroupOption> BitChoiceGroupOptions { get; set; } = new()
        {
            new BitChoiceGroupOption()
            {
                Key = "a",
                Text = "a",
                ImageSrc = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png",
                SelectedImageSrc = "https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png",
                IsEnabled = false
            }
            ,new BitChoiceGroupOption()
            {
                Key = "b",
                Text = "b",
            }
            ,new BitChoiceGroupOption()
            {
                Key = "c",
                Text = "c",
            }
            ,new BitChoiceGroupOption()
            {
                Key = "d",
                Text = "d",
            }
            ,new BitChoiceGroupOption()
            {
                Key = "e",
                Text = "e",
                ImageSrc = "https://upload.wikimedia.org/wikipedia/commons/thumb/a/ab/Female_icon.svg/920px-Female_icon.svg.png",
                SelectedImageSrc = "https://cdn3.iconfinder.com/data/icons/emoticon-2022/100/Zipper-Mouth_Face-512.png",
            },
        };

        private readonly List<ComponentParameter> componentParameters = new()
        {

        };

        private readonly List<EnumParameter> enumParameters = new()
        {

        };
    }
}
