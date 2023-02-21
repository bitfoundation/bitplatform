using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Models;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Pivot;

public partial class BitPivotDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment",
            Description = "The content of pivot, It can be Any custom tag.",
        },
        new()
        {
            Name = "DefaultSelectedKey",
            Type = "string",
            Description = "Default selected key for the pivot.",
        },
        new()
        {
            Name = "LinkFormat",
            Type = "BitLinkFormat",
            DefaultValue = "BitLinkFormat.Tabs",
            LinkType = LinkType.Link,
            Href = "#linkFormat-enum",
            Description = "Pivot link format, display mode for the pivot links.",
        },
        new()
        {
            Name = "LinkSize",
            Type = "BitLinkSize",
            DefaultValue = "BitLinkSize.Normal",
            LinkType = LinkType.Link,
            Href = "#linkSize-enum",
            Description = "Pivot link size.",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitPivotItem>",
            LinkType = LinkType.Link,
            Href = "#pivotItem",
            Description = "Callback for when the a pivot item is clicked.",
        },
        new()
        {
            Name = "OverflowBehavior",
            Type = "BitOverflowBehavior",
            DefaultValue = "BitOverflowBehavior.None",
            LinkType = LinkType.Link,
            Href = "#overflowBehavior-enum",
            Description = "Overflow behavior when there is not enough room to display all of the links/tabs.",
        },
        new()
        {
            Name = "SelectedKey",
            Type = "string",
            Description = "Key of the selected pivot item. Updating this will override the Pivot's selected item state.",
        },
    };

    private readonly List<ComponentSubParameter> componentSubParameters = new()
    {
        new()
        {
            Id = "pivotItem",
            Title = "BitPivotItem",
            Parameters = new()
            {
                new()
                {
                    Name = "Body",
                    Type = "RenderFragment",
                    Description = "The content of the pivot item, It can be Any custom tag or a text (alias of ChildContent).",
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment",
                    Description = "The content of the pivot item, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "Header",
                    Type = "RenderFragment",
                    Description = "The content of the pivot item header, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "HeaderText",
                    Type = "string",
                    Description = "The text of the pivot item header, The text displayed of each pivot link.",
                },
                new()
                {
                    Name = "IconName",
                    Type = "BitIconName",
                    Description = "The icon name for the icon shown next to the pivot link.",
                },
                new()
                {
                    Name = "ItemCount",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Defines an optional item count displayed in parentheses just after the linkText.",
                },
                new()
                {
                    Name = "Key",
                    Type = "string",
                    Description = "A required key to uniquely identify a pivot item.",
                }
            }
        }
    };

    private readonly List<EnumParameter> enumParameters = new()
    {
        new()
        {
            Id = "linkFormat-enum",
            Title = "BitLinkFormat Enum",
            Description = "",
            EnumList = new()
            {
                new()
                {
                    Name= "Tabs",
                    Description="Display Pivot Links as Tabs.",
                    Value="0",
                },
                new()
                {
                    Name= "Links",
                    Description="Display Pivot Links as links.",
                    Value="1",
                },
            }
        },
        new()
        {
            Id = "linkSize-enum",
            Title = "BitLinkSize Enum",
            Description = "",
            EnumList = new()
            {
                new()
                {
                    Name= "Normal",
                    Description="Display Link using normal font size.",
                    Value="0",
                },
                new()
                {
                    Name= "Large",
                    Description="Display links using large font size.",
                    Value="1",
                },
            }
        },
        new()
        {
            Id = "overflowBehavior-enum",
            Title = "BitOverflowBehavior Enum",
            Description = "",
            EnumList = new()
            {
                new()
                {
                    Name= "None",
                    Description="Pivot links will overflow the container and may not be visible.",
                    Value="0",
                },
                new()
                {
                    Name= "Menu",
                    Description="Display an overflow menu that contains the tabs that don't fit.",
                    Value="1",
                },
                new()
                {
                    Name= "Scroll",
                    Description="Display a scroll bar below of the tabs for moving between them.",
                    Value="2",
                },
            }
        },
    };


    private string SelectedKey = "Foo";
    private BitPivotItem SelectedPivotItem;
    private string OverridePivotSelectedKey = "1";
    private BitComponentVisibility PivotItemVisibility;

    private void TogglePivotItemVisibility()
    {
        PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
    }

    private readonly string example1HtmlCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""File"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1</h1>
            <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <div style=""margin-top:15px"">
            <h2>Pivot #2</h2>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <div style=""margin-top:10px"">
            <h3>Pivot #3</h3>
            <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.<br />Suspendisse blandit erat ac lobortis pulvinar. Donec nunc leo, tempus sit amet accumsan in, sagittis sed odio. Pellentesque tristique felis sed purus pellentesque, ac dictum ex fringilla. Integer a tincidunt eros, non porttitor turpis. Sed gravida felis massa, in viverra massa aliquam sit amet. Etiam vitae dolor in velit sodales tristique id nec turpis. Proin sit amet urna sollicitudin, malesuada enim et, lacinia mi. Fusce nisl massa, efficitur sit amet elementum convallis, porttitor vel turpis. Fusce congue dui sit amet mollis pulvinar. Suspendisse vulputate leo quis nunc tincidunt, nec dictum risus congue.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example2HtmlCode = @"
<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""Files"" IconName=""BitIconName.Info"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Files</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Shared with me</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""BitIconName.Info"" ItemCount=""12"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Recent</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Some tab"" IconName=""BitIconName.Info"" ItemCount=""6"">
        <div style=""margin-top:10px"">
            <h1>Pivot #4: Some tab</h1>
            <p>Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Latest"" IconName=""BitIconName.Info"" ItemCount=""8"">
        <div style=""margin-top:10px"">
            <h1>Pivot #5: Latest</h1>
            <p>Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus. Suspendisse blandit erat ac lobortis pulvinar.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example3HtmlCode = @"
<BitPivot LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem HeaderText=""Large File"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Large File</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Shared with me"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Large Shared with me</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Recent"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Large Recent</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example4HtmlCode = @"
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"">
    <BitPivotItem HeaderText=""File tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: File tab</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Shared with me tab</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Recent tab</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example5HtmlCode = @"
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem HeaderText=""Large File tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Large File tab</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Shared with me tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Large Shared with me tab</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Recent tab"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Large Recent tab</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example6HtmlCode = @"
<BitPivot @bind-SelectedKey=""OverridePivotSelectedKey"" LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem Key=""1"" HeaderText=""Samples"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Samples</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""2"" HeaderText=""Files"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Files</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""3"" HeaderText=""Recent"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Recent</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""4"" HeaderText=""Last"">
        <div style=""margin-top:10px"">
            <h1>Pivot #4: Last</h1>
            <p>Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi.</p>
        </div>
    </BitPivotItem>
</BitPivot>
<div style=""margin-top:50px;display:flex;gap:10px"">
    <BitButton IsEnabled=""@(OverridePivotSelectedKey != ""1"")"" ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(() => OverridePivotSelectedKey = (int.Parse(OverridePivotSelectedKey) - 1).ToString())"">
        <div style=""display:flex;gap:2px"">
            <BitIcon IconName=""BitIconName.CaretSolidLeft"" /> Prev
        </div>
    </BitButton>
    <BitButton IsEnabled=""@(OverridePivotSelectedKey != ""4"")"" ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(() => OverridePivotSelectedKey = (int.Parse(OverridePivotSelectedKey) + 1).ToString())"">
        <div style=""display:flex;gap:2px"">
            Next <BitIcon IconName=""BitIconName.CaretSolidRight"" />
        </div>
    </BitButton>
</div>";
    private readonly string example6CSharpCode = @"
private string OverridePivotSelectedKey = ""1"";";

    private readonly string example7HtmlCode = @"
<div style=""margin-bottom:25px;border:1px solid #afafaf;width:fit-content;padding:10px;"">
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
</div>
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs""
            DefaultSelectedKey=""Foo""
            LinkSize=""@BitPivotLinkSize.Large""
            HeadersOnly=""true""
            OnLinkClick=""@(item => SelectedKey = item.Key)"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo""></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Key=""Bar""></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""Bas""></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" Key=""Biz""></BitPivotItem>
</BitPivot>";
    private readonly string example7CSharpCode = @"
private string SelectedKey = ""Foo"";";

    private readonly string example8HtmlCode = @"
<div style=""margin-bottom:25px"">
    Last Pivot clicked: <strong>@SelectedPivotItem?.HeaderText</strong>
</div>
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitPivotLinkSize.Large"" OnLinkClick=""@(item => SelectedPivotItem = item)"">
    <BitPivotItem HeaderText=""Foo"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Foo</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bar"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Bar</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""aaa"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Bas</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem Visibility=""@PivotItemVisibility"" HeaderText=""Biz"">
        <div style=""margin-top:10px"">
            <h1>Pivot #4: Biz</h1>
            <p>Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Baq"">
        <div style=""margin-top:10px"">
            <h1>Pivot #5: Baq</h1>
            <p>Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus. Suspendisse blandit erat ac lobortis pulvinar.</p>
        </div>
    </BitPivotItem>
</BitPivot>";
    private readonly string example8CSharpCode = @"
private BitPivotItem SelectedPivotItem;";

    private readonly string example9HtmlCode = @"
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitPivotLinkSize.Large"">
    <BitPivotItem HeaderText=""Foo"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1: Foo</h1>
            <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bar"">
        <div style=""margin-top:10px"">
            <h1>Pivot #2: Bar</h1>
            <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem Visibility=""@PivotItemVisibility"" HeaderText=""Biz"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Biz</h1>
            <p>Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan.</p>
        </div>
    </BitPivotItem>
</BitPivot>
<div style=""margin-top:50px"">
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""TogglePivotItemVisobility"">Hide/Show Biz</BitButton>
</div>";
    private readonly string example9CSharpCode = @"
private BitComponentVisibility PivotItemVisibility;
private void TogglePivotItemVisobility()
{
    PivotItemVisibility = PivotItemVisibility == BitComponentVisibility.Visible ? BitComponentVisibility.Collapsed : BitComponentVisibility.Visible;
}";

    private readonly string example10HtmlCode = @"
<BitPivot>
    <BitPivotItem>
        <Header>
            <div>
                <span style=""color:red"">Header #1</span>
            </div>
        </Header>
        <Body>
            <div style=""margin-top:10px"">
                <h1>Pivot #1</h1>
                <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra.</p>
            </div>
        </Body>
    </BitPivotItem>
    <BitPivotItem ItemCount=""99"">
        <Header>
            <div>
                <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
                <span style=""color:blue"">Header #2</span>
                <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
            </div>
        </Header>
        <Body>
            <div style=""margin-top:10px"">
                <h1>Pivot #2</h1>
                <p>Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel.</p>
            </div>
        </Body>
    </BitPivotItem>
    <BitPivotItem IconName=""BitIconName.Inbox"">
        <Header>
            <div>
                <span style=""color:rebeccapurple"">Header <i style=""color:purple"" class=""bit-icon bit-icon--HeartFill""></i> #3</span>
            </div>
        </Header>
        <Body>
            <div style=""margin-top:10px"">
                <h1>Pivot #3</h1>
                <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
            </div>
        </Body>
    </BitPivotItem>
</BitPivot>";

    private readonly string example11HtmlCode = @"
<style>
.subtitle {
    padding: 20px 0 10px 0;
}

.box {
    border: 1px solid #ccc;
    padding: 5px 10px;
}
</style>

<div class=""subtitle"">Pivot Position: <strong>Top</strong></div>
<div class=""box"">
    <BitPivot Position=""BitPivotPosition.Top"" Style=""height:200px"">
        <BitPivotItem HeaderText=""File"">
            <div style=""margin-top:10px"">
                <h1>Pivot #1: File</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. </p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Shared"">
            <div style=""margin-top:15px"">
                <h1>Pivot #2: Shared</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet.</p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Recent"">
            <div style=""margin-top:10px"">
                <h1>Pivot #3: Recent</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. </p>
            </div>
        </BitPivotItem>
    </BitPivot>
</div>
<br />
<div class=""subtitle"">Pivot Position: <strong>Bottom</strong></div>
<div class=""box"">
    <BitPivot Position=""BitPivotPosition.Bottom"" Style=""height:200px"">
        <BitPivotItem HeaderText=""File"">
            <div style=""margin-top:10px"">
                <h1>Pivot #1: File</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. </p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Shared"">
            <div style=""margin-top:15px"">
                <h1>Pivot #2: Shared</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet.</p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Recent"">
            <div style=""margin-top:10px"">
                <h1>Pivot #3: Recent</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. </p>
            </div>
        </BitPivotItem>
    </BitPivot>
</div>
<br />
<div class=""subtitle"">Pivot Position: <strong>Left</strong></div>
<div class=""box"">
    <BitPivot Position=""BitPivotPosition.Left"">
        <BitPivotItem HeaderText=""File"">
            <div style=""margin-top:10px"">
                <h1>Pivot #1: File</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. </p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me"" Style=""width:130px"">
            <div style=""margin-top:15px"">
                <h1>Pivot #2: Shared with me</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet.</p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Recent"">
            <div style=""margin-top:10px"">
                <h1>Pivot #3: Recent</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. </p>
            </div>
        </BitPivotItem>
    </BitPivot>
</div>
<br />
<div class=""subtitle"">Pivot Position: <strong>Right</strong></div>
<div class=""box"">
    <BitPivot Position=""BitPivotPosition.Right"">
        <BitPivotItem HeaderText=""File"">
            <div style=""margin-top:10px"">
                <h1>Pivot #1: File</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. </p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Shared with me"" Style=""width:130px"">
            <div style=""margin-top:15px"">
                <h1>Pivot #2: Shared with me</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet.</p>
            </div>
        </BitPivotItem>
        <BitPivotItem HeaderText=""Recent"">
            <div style=""margin-top:10px"">
                <h1>Pivot #3: Recent</h1>
                <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. </p>
            </div>
        </BitPivotItem>
    </BitPivot>
</div>";
}
