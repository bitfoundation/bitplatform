using System;
using System.Collections.Generic;
using Bit.Client.Web.BlazorUI.Playground.Web.Models;
using Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.Client.Web.BlazorUI.Playground.Web.Pages.Components.Pivot
{
    public partial class BitPivotDemo
    {
        private string SelectedKey = "Foo";
        private BitPivotItem SelectedPivotItem;
        private string OverridePivotSelectedKey = "1";
        private BitComponentVisibility PivotItemVisibility;

        private readonly List<ComponentParameter> componentParameters = new()
        {
            new ComponentParameter()
            {
                Name = "ChildContent",
                Type = "RenderFragment",
                DefaultValue = "",
                Description = "The content of pivot, It can be Any custom tag.",
            },
            new ComponentParameter()
            {
                Name = "DefaultSelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Default selected key for the pivot.",
            },
            new ComponentParameter()
            {
                Name = "LinkFormat",
                Type = "BitLinkFormat",
                DefaultValue = "BitLinkFormat.Tabs",
                LinkType = LinkType.Link,
                Href = "#linkFormat-enum",
                Description = "Pivot link format, display mode for the pivot links.",
            },
            new ComponentParameter()
            {
                Name = "LinkSize",
                Type = "BitLinkSize",
                DefaultValue = "BitLinkSize.Normal",
                LinkType = LinkType.Link,
                Href = "#linkSize-enum",
                Description = "Pivot link size.",
            },
            new ComponentParameter()
            {
                Name = "OnLinkClick",
                Type = "EventCallback<BitPivotItem>",
                DefaultValue = "",
                LinkType = LinkType.Link,
                Href = "#pivotItem",
                Description = "Callback for when the selected pivot item is changed.",
            },
            new ComponentParameter()
            {
                Name = "OverflowBehavior",
                Type = "BitOverflowBehavior",
                DefaultValue = "BitOverflowBehavior.None",
                LinkType = LinkType.Link,
                Href = "#overflowBehavior-enum",
                Description = "Overflow behavior when there is not enough room to display all of the links/tabs.",
            },
            new ComponentParameter()
            {
                Name = "SelectedKey",
                Type = "string",
                DefaultValue = "",
                Description = "Key of the selected pivot item. Updating this will override the Pivot's selected item state.",
            },
        };

        private readonly List<ComponentSubParameter> componentSubParameters = new()
        {
            new ComponentSubParameter()
            {
                Id = "pivotItem",
                Title = "BitPivotItem",
                Parameters = new List<ComponentParameter>()
                {
                   new ComponentParameter()
                   {
                       Name = "ChildContent",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item, It can be Any custom tag or a text.",
                   },
                   new ComponentParameter()
                   {
                       Name = "BodyFragment",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item can be Any custom tag or a text, If HeaderContent provided value of this parameter show, otherwise use ChildContent.",
                   },
                   new ComponentParameter()
                   {
                       Name = "HeaderFragment",
                       Type = "RenderFragment",
                       DefaultValue = "",
                       Description = "The content of the pivot item header, It can be Any custom tag or a text.",
                   },
                   new ComponentParameter()
                   {
                       Name = "HeaderText",
                       Type = "string",
                       DefaultValue = "",
                       Description = "The text of the pivot item header, The text displayed of each pivot link.",
                   },
                   new ComponentParameter()
                   {
                       Name = "IconName",
                       Type = "BitIcon",
                       DefaultValue = "",
                       Description = "The icon name for the icon shown next to the pivot link.",
                   },
                   new ComponentParameter()
                   {
                       Name = "ItemCount",
                       Type = "int",
                       DefaultValue = "0",
                       Description = "Defines an optional item count displayed in parentheses just after the linkText.",
                   },
                   new ComponentParameter()
                   {
                       Name = "Key",
                       Type = "string",
                       DefaultValue = "",
                       Description = "A required key to uniquely identify a pivot item.",
                   },
                }
            }
        };

        private readonly List<EnumParameter> enumParameters = new()
        {
            new EnumParameter()
            {
                Id = "linkFormat-enum",
                Title = "BitLinkFormat Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Tabs",
                        Description="Display Pivot Links as Tabs.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Links",
                        Description="Display Pivot Links as links.",
                        Value="1",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "linkSize-enum",
                Title = "BitLinkSize Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "Normal",
                        Description="Display Link using normal font size.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Large",
                        Description="Display links using large font size.",
                        Value="1",
                    },
                }
            },
            new EnumParameter()
            {
                Id = "overflowBehavior-enum",
                Title = "BitOverflowBehavior Enum",
                Description = "",
                EnumList = new List<EnumItem>()
                {
                    new EnumItem()
                    {
                        Name= "None",
                        Description="Pivot links will overflow the container and may not be visible.",
                        Value="0",
                    },
                    new EnumItem()
                    {
                        Name= "Menu",
                        Description="Display an overflow menu that contains the tabs that don't fit.",
                        Value="1",
                    },
                    new EnumItem()
                    {
                        Name= "Scroll",
                        Description="Display a scroll bar below of the tabs for moving between them.",
                        Value="2",
                    },
                }
            }
        };

        private readonly string pivotSampleCode = $"<BitPivot>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='File'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Shared with me'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Recent'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>";

        private readonly string pivotWithCountAndIconSampleCode = $"<BitPivot OverflowBehavior='@OverflowBehavior.Scroll'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='Files' IconName='BitIcon.Info'>Pivot #1</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='Shared with me' ItemCount='32'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='Recent' IconName='BitIcon.Info' ItemCount='12'>Pivot #3</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='Some tab' IconName='BitIcon.Info' ItemCount='6'>Pivot #4</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='Latest' IconName='BitIcon.Info' ItemCount='8'>Pivot #5</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText ='I also have a custom template for icon' IconName='BitIcon.Info' ItemCount='45'></BitPivotItem>{Environment.NewLine}" +
             $"<HeaderContent>{Environment.NewLine}" +
             $"<i class='bit-icon bit-icon--HeartFill'></i>{Environment.NewLine}" +
             $"</HeaderContent>{Environment.NewLine}" +
             $"<BodyContent>{Environment.NewLine}" +
             $"Pivot #6{Environment.NewLine}" +
             $"</BodyContent>{Environment.NewLine}" +
             $"</BitPivot>";

        private readonly string pivotWithLargeLinkSizeSampleCode = $"<BitPivot LinkSize='@LinkSize.Large'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='File'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Shared with me'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Recent'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>";

        private readonly string pivotWithLinkSampleCode = $"<BitPivot LinkFormat='@LinkFormat.Tabs'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Foo'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bar'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bas'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Biz'>Pivot #4></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>";

        private readonly string pivotWithLargeLinkSampleCode = $"<BitPivot LinkFormat='@LinkFormat.Tabs' LinkSize='@LinkSize.Large'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Foo'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bar'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bas'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Biz'>Pivot #4></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>";

        private readonly string pivotChangeFromOutsideSampleCode = $"<BitPivot LinkFormat='@LinkFormat.Tabs' LinkSize='@LinkSize.Large' SelectedKey='@OverridePivotSelectedKey' SelectedKeyChanged='PivotSelectedKeyChanged'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Samples'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Files'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Recent'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Biz'>Pivot #4></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>{Environment.NewLine}" +
             $"<BitButton OnClick='(() => OverridePivotSelectedKey = (((int.Parse(OverridePivotSelectedKey) + 3 - 1) % 3)).ToString())'>prev</BitButton>{Environment.NewLine}" +
             $"<BitButton OnClick='(() => OverridePivotSelectedKey = (((int.Parse(OverridePivotSelectedKey) + 1 ) % 3)).ToString())'>next</BitButton>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"public string OverridePivotSelectedKey {{ get; set; }} = '1';{Environment.NewLine}" +
             $"public void PivotSelectedKeyChanged(string key){Environment.NewLine}" +
             $"{{{Environment.NewLine}" +
             $"OverridePivotSelectedKey = key;{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"}}";

        private readonly string pivotContent1SampleCode = $"<BitPivot LinkFormat='@LinkFormat.Tabs' LinkSize='@LinkSize.Large' OnLinkClick='@(item => SelectedKey = item.Key)'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Foo'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bar'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bas'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Biz'>Pivot #4></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>{Environment.NewLine}" +
             $"@if (SelectedKey == 'Foo'){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"<div>Hello I am Foo</div>{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"@if (SelectedKey == 'Bar'){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"<div>Hello I am Bar</div>{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"@if (SelectedKey == 'Bas'){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"<div>Hello I am Bas</div>{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"@if (SelectedKey == 'Biz'){Environment.NewLine}" +
             $"{{ {Environment.NewLine}" +
             $"<div>Hello I am Biz</div>{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"private string SelectedKey = 'Foo';{Environment.NewLine}" +
             $"}}";

        private readonly string pivotContent2SampleCode = $"<span>@SelectedPivotItem?.HeaderText Clicked</span>{Environment.NewLine}" +
             $"<BitPivot LinkFormat='@LinkFormat.Tabs' LinkSize='@LinkSize.Large' OnLinkClick='@(item => SelectedPivotItem = item)'>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Foo'>Pivot #1></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bar'>Pivot #2</BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Bas' Key='aaa'>Pivot #3></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Biz' Visibility='@PivotItemVisibility'>Pivot #4></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Baq'>Pivot #5></BitPivotItem>{Environment.NewLine}" +
             $"<BitPivotItem HeaderText='Baw'>Pivot #6></BitPivotItem>{Environment.NewLine}" +
             $"</BitPivot>{Environment.NewLine}" +
             $"<BitButton OnClick='TogglePivotItemVisobility'>Hide/Show Biz</BitButton>{Environment.NewLine}" +
             $"@code {{ {Environment.NewLine}" +
             $"public ComponentVisibility PivotItemVisibility {{ get; set; }}{Environment.NewLine}" +
             $"public BitPivotItem SelectedPivotItem {{ get; set; }}{Environment.NewLine}" +
             $"public void TogglePivotItemVisobility(){Environment.NewLine}" +
             $"{{{Environment.NewLine}" +
             $"PivotItemVisibility = PivotItemVisibility == ComponentVisibility.Visible ? ComponentVisibility.Collapsed : ComponentVisibility.Visible;{Environment.NewLine}" +
             $"}}{Environment.NewLine}" +
             $"}}";

        private void PivotSelectedKeyChanged(string key)
        {
            OverridePivotSelectedKey = key;
        }

        private void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
        }
    }
}
