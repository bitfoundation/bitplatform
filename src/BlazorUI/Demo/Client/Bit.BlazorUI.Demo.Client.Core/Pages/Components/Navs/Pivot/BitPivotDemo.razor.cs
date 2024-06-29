namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pivot;

public partial class BitPivotDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of pivot, It can be Any custom tag.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPivot component.",
            Href = "#pivot-class-styles",
            LinkType = LinkType.Link
        },
        new()
        {
            Name = "DefaultSelectedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "Default selected key for the pivot.",
        },
        new()
        {
            Name = "HeadersOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to skip rendering the tabpanel with the content of the selected tab.",
        },
        new()
        {
            Name = "LinkFormat",
            Type = "BitPivotLinkFormat",
            DefaultValue = "BitPivotLinkFormat.Links",
            Description = "Pivot link format, display mode for the pivot links.",
            LinkType = LinkType.Link,
            Href = "#linkFormat-enum",
        },
        new()
        {
            Name = "LinkSize",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the pivot links.",
            LinkType = LinkType.Link,
            Href = "#linkSize-enum",
        },
        new()
        {
            Name = "OverflowBehavior",
            Type = "BitOverflowBehavior",
            DefaultValue = "BitOverflowBehavior.None",
            Description = "Overflow behavior when there is not enough room to display all of the links/tabs.",
            LinkType = LinkType.Link,
            Href = "#overflowBehavior-enum",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when the a pivot item is clicked.",
            LinkType = LinkType.Link,
            Href = "#pivotItem",
        },
        new()
        {
            Name = "Position",
            Type = "BitPivotPosition",
            DefaultValue = "BitPivotPosition.Top",
            Description = "Position of the pivot header.",
            LinkType = LinkType.Link,
            Href = "#pivotPosition-enum",
        },
        new()
        {
            Name = "SelectedKey",
            Type = "string?",
            DefaultValue = "null",
            Description = "Key of the selected pivot item. Updating this will override the Pivot's selected item state.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPivot component.",
            Href = "#pivot-class-styles",
            LinkType = LinkType.Link
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pivotItem",
            Title = "BitPivotItem",
            Parameters =
            [
                new()
                {
                    Name = "Body",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item, It can be Any custom tag or a text (alias of ChildContent).",
                },
                new()
                {
                    Name = "ChildContent",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "Header",
                    Type = "RenderFragment?",
                    DefaultValue = "null",
                    Description = "The content of the pivot item header, It can be Any custom tag or a text.",
                },
                new()
                {
                    Name = "HeaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The text of the pivot item header, The text displayed of each pivot link.",
                },
                new()
                {
                    Name = "IconName",
                    Type = "string?",
                    DefaultValue = "null",
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
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "A required key to uniquely identify a pivot item.",
                }
            ]
        },
        new()
        {
            Id = "pivot-class-styles",
            Title = "BitPivotClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitPivot."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitPivot."
               },
               new()
               {
                   Name = "ContentView",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the content view of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item of the BitPivot."
               },
               new()
               {
                   Name = "SelectedItem",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the selected item of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItemContent",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item content of the BitPivot."
               },
               new()
               {
                   Name = "HeaderIconContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header icon container of the BitPivot."
               },
               new()
               {
                   Name = "HeaderIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header icon of the BitPivot."
               },
               new()
               {
                   Name = "HeaderText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header text of the BitPivot."
               },
               new()
               {
                   Name = "HeaderItemCount",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header item count of the BitPivot."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "linkFormat-enum",
            Name = "BitLinkFormat",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "linkSize-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="Display Link using small font size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="Display Link using medium font size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="Display links using large font size.",
                    Value="2",
                },
            ]
        },
        new()
        {
            Id = "overflowBehavior-enum",
            Name = "BitOverflowBehavior",
            Description = "",
            Items =
            [
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
            ]
        },
        new()
        {
            Id = "pivotPosition-enum",
            Name = "BitPivotPosition",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="Display header at the top.",
                    Value="0",
                },
                new()
                {
                    Name= "Bottom",
                    Description="Display header at the Bottom.",
                    Value="1",
                },
                new()
                {
                    Name= "Left",
                    Description="Display header at the Left.",
                    Value="2",
                },
                new()
                {
                    Name= "Right",
                    Description="Display header at the Right.",
                    Value="3",
                },
            ]
        },
    ];



    private readonly string example1RazorCode = @"
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

    private readonly string example2RazorCode = @"
<BitPivot OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.Info"">
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
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Info"" ItemCount=""12"">
        <div style=""margin-top:10px"">
            <h1>Pivot #3: Recent</h1>
            <p>Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Some tab"" IconName=""@BitIconName.Info"" ItemCount=""6"">
        <div style=""margin-top:10px"">
            <h1>Pivot #4: Some tab</h1>
            <p>Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Latest"" IconName=""@BitIconName.Info"" ItemCount=""8"">
        <div style=""margin-top:10px"">
            <h1>Pivot #5: Latest</h1>
            <p>Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus. Suspendisse blandit erat ac lobortis pulvinar.</p>
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example3RazorCode = @"
<BitPivot LinkSize=""@BitSize.Large"">
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

    private readonly string example4RazorCode = @"
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

    private readonly string example5RazorCode = @"
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitSize.Large"">
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

    private readonly string example6RazorCode = @"
<BitPivot @bind-SelectedKey=""OverridePivotSelectedKey"" LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitSize.Large"">
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
            <BitIcon IconName=""@BitIconName.CaretSolidLeft"" /> Prev
        </div>
    </BitButton>
    <BitButton IsEnabled=""@(OverridePivotSelectedKey != ""4"")"" ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(() => OverridePivotSelectedKey = (int.Parse(OverridePivotSelectedKey) + 1).ToString())"">
        <div style=""display:flex;gap:2px"">
            Next <BitIcon IconName=""@BitIconName.CaretSolidRight"" />
        </div>
    </BitButton>
</div>";
    private readonly string example6CsharpCode = @"
private string OverridePivotSelectedKey = ""1"";";

    private readonly string example7RazorCode = @"
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
            LinkSize=""@BitSize.Large""
            HeadersOnly=""true""
            OnItemClick=""@(item => SelectedKey = item.Key)"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo""></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Key=""Bar""></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""Bas""></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" Key=""Biz""></BitPivotItem>
</BitPivot>";
    private readonly string example7CsharpCode = @"
private string SelectedKey = ""Foo"";";

    private readonly string example8RazorCode = @"
<div style=""margin-bottom:25px"">
    Last Pivot clicked: <strong>@SelectedPivotItem?.HeaderText</strong>
</div>

<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitSize.Large"" OnItemClick=""@(item => SelectedPivotItem = item)"">
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
    private readonly string example8CsharpCode = @"
private BitPivotItem SelectedPivotItem;";

    private readonly string example9RazorCode = @"
<BitPivot LinkFormat=""@BitPivotLinkFormat.Tabs"" LinkSize=""@BitSize.Large"">
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
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""TogglePivotItemVisibility"">Hide/Show Biz</BitButton>
</div>";
    private readonly string example9CsharpCode = @"
private BitVisibility PivotItemVisibility;
private void TogglePivotItemVisibility()
{
    PivotItemVisibility = PivotItemVisibility == BitVisibility.Visible ? BitVisibility.Collapsed : BitVisibility.Visible;
}";

    private readonly string example10RazorCode = @"
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
    <BitPivotItem IconName=""@BitIconName.Inbox"">
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

    private readonly string example11RazorCode = @"
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

    private readonly string example12RazorCode = @"
<BitButton OnClick=""() => PivotEnabled = !PivotEnabled"">Toggle Pivot's IsEnabled</BitButton>
<BitButton OnClick=""() => PivotItemEnabled = !PivotItemEnabled"">Toggle Pivot Item's IsEnabled</BitButton>

<BitPivot IsEnabled=""PivotEnabled"">
    <BitPivotItem HeaderText=""File"">
        <div style=""margin-top:10px"">
            <h1>Pivot #1</h1>
            <p style=""white-space:pre-wrap"">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.</p>
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" IsEnabled=""PivotItemEnabled"">
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
    private readonly string example12CsharpCode = @"
private bool PivotEnabled = true;
private bool PivotItemEnabled = true;";

    private readonly string example13RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        padding-left: 0.25rem;
        box-shadow: 0 0 1rem lightskyblue;
    }

    .custom-selected-item {
        background-color: goldenrod;
    }

    .custom-header {
        overflow: hidden;
        border-radius: 1rem;
        border: 1px solid gray;
    }

    .custom-content-view {
        margin-top: 1rem;
        background-color: deepskyblue;
    }
</style>


<BitPivot Style=""border: 1px solid tomato;"">
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
</BitPivot>

<BitPivot Class=""custom-class"">
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
</BitPivot>

<BitPivot Styles=""@(new() { HeaderIcon = ""color: tomato;"", HeaderText = ""color: purple;"" })"">
    <BitPivotItem HeaderText=""File"" IconName=""Info"">
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
</BitPivot>

<BitPivot Classes=""@(new() { ContentView = ""custom-content-view"", SelectedItem = ""custom-selected-item"", Header = ""custom-header"" })"">
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

    private readonly string example14RazorCode = @"
<BitPivot Dir=""BitDir.Rtl"" OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""اسناد"" IconName=""@BitIconName.Info"">
        <br />
        <p>
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        </p>
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"" IconName=""@BitIconName.Info"" ItemCount=""8"">
        <br />
        <p>
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        </p>
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"" IconName=""@BitIconName.Info"" ItemCount=""6"">
        <br />
        <p>
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        </p>
    </BitPivotItem>
    <BitPivotItem HeaderText=""اخیرا"" IconName=""@BitIconName.Info"" ItemCount=""12"">
        <br />
        <p>
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </p>
    </BitPivotItem>
</BitPivot>";
}
