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
                       Type = "BitIconName",
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
            },
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

        private void TogglePivotItemVisobility()
        {
            PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
        }

        private readonly string example1HTMLCode = @"<BitPivot>
    <BitPivotItem HeaderText=""File"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        Pivot #3
    </BitPivotItem>
</BitPivot>";

        private readonly string example2HTMLCode = @"<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""Files""
                  IconName=""BitIconName.Info"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me""
                  ItemCount=""32"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent""
                  IconName=""BitIconName.Info""
                  ItemCount=""12"">
        Pivot #3
    </BitPivotItem>
    <BitPivotItem HeaderText=""Some tab""
                  IconName=""BitIconName.Info""
                  ItemCount=""6"">
        Pivot #4
    </BitPivotItem>
    <BitPivotItem HeaderText=""Latest""
                  IconName=""BitIconName.Info""
                  ItemCount=""8"">
        Pivot #5
    </BitPivotItem>
    <BitPivotItem HeaderText=""I also have a custom template for icon""
                  IconName=""BitIconName.Info""
                  ItemCount=""45"">
        <HeaderFragment>
            <i class=""bit-icon bit-icon--HeartFill""></i>
        </HeaderFragment>
        <BodyFragment>
            Pivot #6
        </BodyFragment>
    </BitPivotItem>
</BitPivot>";

        private readonly string example3HTMLCode = @"<BitPivot LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem HeaderText=""File"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        Pivot #3
    </BitPivotItem>
</BitPivot>";

        private readonly string example4HTMLCode = @"<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"">
    <BitPivotItem HeaderText=""Foo"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bar"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bas"">
        Pivot #3
    </BitPivotItem>
    <BitPivotItem HeaderText=""Biz"">
        Pivot #4
    </BitPivotItem>
</BitPivot>";

        private readonly string example5HTMLCode = @"<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs""
          LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem HeaderText=""Foo"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bar"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bas"">
        Pivot #3
    </BitPivotItem>
    <BitPivotItem HeaderText=""Biz"">
        Pivot #4
    </BitPivotItem>
</BitPivot>";

        private readonly string example6HTMLCode = @"<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs""
          LinkSize=""@BitPivotLinkSize.Large""
          @bind-SelectedKey=""OverridePivotSelectedKey"">
    <BitPivotItem Key=""1"" HeaderText=""Samples"">
        Pivot #1
    </BitPivotItem>
    <BitPivotItem Key=""2"" HeaderText=""Files"">
        Pivot #2
    </BitPivotItem>
    <BitPivotItem Key=""3"" HeaderText=""Recent"">
        Pivot #3
    </BitPivotItem>
</BitPivot>
<BitButton IsEnabled=""@(OverridePivotSelectedKey != ""1"")""
           OnClick=""() => OverridePivotSelectedKey = (((int.Parse(OverridePivotSelectedKey) + 3 - 1) % 3)).ToString()"">Prev</BitButton>
<BitButton IsEnabled=""@(OverridePivotSelectedKey != ""3"")""
           OnClick=""() => OverridePivotSelectedKey = (((int.Parse(OverridePivotSelectedKey) + 1 ) % 4)).ToString()"">Next</BitButton>";

        private readonly string example6CSharpCode = @"
private string OverridePivotSelectedKey = ""1"";";

        private readonly string example7HTMLCode = @"<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs""
          DefaultSelectedKey=""Foo""
          LinkSize=""@BitPivotLinkSize.Large""
          HeadersOnly=""true""
          OnLinkClick=""@(item => SelectedKey = item.Key)"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo""></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Key=""Bar""></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""Bas""></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" Key=""Biz""></BitPivotItem>
</BitPivot>
<div>
    @if (SelectedKey == ""Foo"")
    {
        <div>Hello I am Foo</div>
    }
    else if (SelectedKey == ""Bar"")
    {
        <div>Hello I am Bar</div>
    }
    else if (SelectedKey == ""Bas"")
    {
        <div>Hello I am Bas</div>
    }
    else if (SelectedKey == ""Biz"")
    {
        <div>Hello I am Biz</div>
    }
</div>";

        private readonly string example7CSharpCode = @"
private string SelectedKey = ""Foo"";";

        private readonly string example8HTMLCode = @"<div>
    <BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs""
              LinkSize=""@BitPivotLinkSize.Large""
              OnLinkClick=""@(item => SelectedPivotItem = item)"">
        <BitPivotItem HeaderText=""Foo"">
            Pivot #1
        </BitPivotItem>
        <BitPivotItem HeaderText=""Bar"">
            Pivot #2
        </BitPivotItem>
        <BitPivotItem HeaderText=""Bas"" Key=""aaa"">
            Pivot #3
        </BitPivotItem>
        <BitPivotItem Visibility=""@PivotItemVisibility"" HeaderText=""Biz"">
            Pivot #4
        </BitPivotItem>
        <BitPivotItem HeaderText=""Baq"">
            Pivot #5
        </BitPivotItem>
        <BitPivotItem HeaderText=""Baw"">
            Pivot #6
        </BitPivotItem>
    </BitPivot>
</div>
<BitButton OnClick=""TogglePivotItemVisobility"">Hide/Show Biz</BitButton>";

        private readonly string example8CSharpCode = @"
private BitPivotItem SelectedPivotItem;
private BitComponentVisibility PivotItemVisibility;
private void TogglePivotItemVisobility()
{
    PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
}";
    }
}
