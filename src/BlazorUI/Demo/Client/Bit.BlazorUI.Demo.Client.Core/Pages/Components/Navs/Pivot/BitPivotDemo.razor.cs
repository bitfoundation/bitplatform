namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Pivot;

public partial class BitPivotDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alignment",
            Type = "BitAlignment?",
            DefaultValue = "null",
            Description = "Determines the alignment of the header section of the pivot.",
            LinkType = LinkType.Link,
            Href = "#alignment-enum",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of pivot.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the pivot.",
            LinkType = LinkType.Link,
            Href = "#pivot-class-styles",
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
            Name = "HeaderOnly",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether to skip rendering the tabpanel with the content of the selected tab.",
        },
        new()
        {
            Name = "HeaderType",
            Type = "BitPivotHeaderType",
            DefaultValue = "BitPivotHeaderType.Link",
            Description = "The type of the pivot header items.",
            LinkType = LinkType.Link,
            Href = "#header-type-enum",
        },
        new()
        {
            Name = "MountAll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Mounts all tabs at render time and hide non-selected tabs with CSS styles instead of not-rendering them (useful for processing/extracting data).",
        },
        new()
        {
            Name = "OnItemClick",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when the a pivot item is clicked.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<BitPivotItem>",
            Description = "Callback for when the selected pivot item changes.",
            LinkType = LinkType.Link,
            Href = "#pivot-item",
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
            Description = "Key of the selected pivot item.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The size of the pivot header items.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPivotClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the pivot.",
            Href = "#pivot-class-styles",
            LinkType = LinkType.Link
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "pivot-item",
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
                    Name = "BodyClass",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom css class of the content of the pivot item.",
                },
                new()
                {
                    Name = "BodyStyle",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The custom css style of the content of the pivot item.",
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
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the items body of the BitPivot."
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
            Id = "alignment-enum",
            Name = "BitAlignment",
            Description = "",
            Items =
            [
                new()
                {
                    Name = "Start",
                    Value = "0",
                },
                new()
                {
                    Name = "End",
                    Value = "1",
                },
                new()
                {
                    Name = "Center",
                    Value = "2",
                },
                new()
                {
                    Name = "SpaceBetween",
                    Value = "3",
                },
                new()
                {
                    Name = "SpaceAround",
                    Value = "4",
                },
                new()
                {
                    Name = "SpaceEvenly",
                    Value = "5",
                },
                new()
                {
                    Name = "Baseline",
                    Value = "6",
                },
                new()
                {
                    Name = "Stretch",
                    Value = "7",
                }
            ]
        },
        new()
        {
            Id = "header-type-enum",
            Name = "BitPivotHeaderType",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Tab",
                    Description="Renders pivot header items as Tab.",
                    Value="0",
                },
                new()
                {
                    Name= "Link",
                    Description="Renders pivot header items as link.",
                    Value="1",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size.",
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
                    Name= "Start",
                    Description="Display header at the start (Left for LTR and Right for RTL).",
                    Value="2",
                },
                new()
                {
                    Name= "End",
                    Description="Display header at the end (Right for LTR and Left for RTL).",
                    Value="3",
                },
            ]
        },
    ];



    private string selectedKey = "1";
    private string? detachedSelectedKey = "Foo";
    private BitPivotItem selectedPivotItem = default!;



    private readonly string example1RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""File"">
        <h3>Pivot #1</h3>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h3>Pivot #2</h3>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example2RazorCode = @"
<BitPivot>
    <BitPivotItem HeaderText=""Files"" IconName=""@BitIconName.Info"">
        <h1>Pivot #1: Files</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" ItemCount=""32"">
        <h1>Pivot #2: Shared with me</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"" IconName=""@BitIconName.Info"" ItemCount=""12"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example3RazorCode = @"
<BitPivot Size=""@BitSize.Large"">
    <BitPivotItem HeaderText=""Large File"">
        <h1>Pivot #1: Large File</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Shared with me"">
        <h1>Pivot #2: Large Shared with me</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Large Recent"">
        <h1>Pivot #3: Large Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example4RazorCode = @"
<BitPivot HeaderType=""@BitPivotHeaderType.Tab"">
    <BitPivotItem HeaderText=""File tab"">
        <h1>Pivot #1: File tab</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me tab"">
        <h1>Pivot #2: Shared with me tab</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent tab"">
        <h1>Pivot #3: Recent tab</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example5RazorCode = @"
<BitPivot @bind-SelectedKey=""selectedKey"">
    <BitPivotItem Key=""1"" HeaderText=""Samples"">
        <h1>Pivot #1: Samples</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""2"" HeaderText=""Files"">
        <h1>Pivot #2: Files</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""3"" HeaderText=""Recent"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
    <BitPivotItem Key=""4"" HeaderText=""Last"">
        <h1>Pivot #4: Last</h1>
        <div>
            In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
            each word has the power to transform into something extraordinary. Here lies the start of something new—an
            opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
            idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
            begins here, in this quiet moment where everything is possible.
        </div>
    </BitPivotItem>
</BitPivot>

<BitButton Variant=""BitVariant.Outline""
            IconName=""@BitIconName.CaretSolidLeft""
            IsEnabled=""@(selectedKey != ""1"")""
            OnClick=""(() => selectedKey = (int.Parse(selectedKey) - 1).ToString())"">
    Prev
</BitButton>
<BitButton Variant=""BitVariant.Outline""
            IconName=""@BitIconName.CaretSolidRight""
            IsEnabled=""@(selectedKey != ""4"")""
            OnClick=""(() => selectedKey = (int.Parse(selectedKey) + 1).ToString())"">
    Next
</BitButton>";
    private readonly string example5CsharpCode = @"
private string selectedKey = ""1"";";

    private readonly string example6RazorCode = @"
<div style=""border:1px solid gray;padding:10px;"">
    @if (detachedSelectedKey == ""Foo"")
    {
        <div>Hello I am Fooooooooooooo</div>
    }
    else if (detachedSelectedKey == ""Bar"")
    {
        <div>Hello I am Barrrrrrrrrrrr</div>
    }
    else if (detachedSelectedKey == ""Bas"")
    {
        <div>Hello I am Bassssssssssss</div>
    }
    else if (detachedSelectedKey == ""Biz"")
    {
        <div>Hello I am Bizzzzzzzzzzzz</div>
    }
</div>
<hr />
<BitPivot HeaderOnly=""true""
          DefaultSelectedKey=""Foo""
          OnItemClick=""@(item => detachedSelectedKey = item?.Key)"">
    <BitPivotItem HeaderText=""Foo"" Key=""Foo""></BitPivotItem>
    <BitPivotItem HeaderText=""Bar"" Key=""Bar""></BitPivotItem>
    <BitPivotItem HeaderText=""Bas"" Key=""Bas""></BitPivotItem>
    <BitPivotItem HeaderText=""Biz"" Key=""Biz""></BitPivotItem>
</BitPivot>";
    private readonly string example6CsharpCode = @"
private string detachedSelectedKey = ""Foo"";";

    private readonly string example7RazorCode = @"
<BitPivot OnItemClick=""@(item => selectedPivotItem = item)"">
    <BitPivotItem HeaderText=""Foo"">
        <h1>Pivot #1: Foo</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bar"">
        <h1>Pivot #2: Bar</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Bas"">
        <h1>Pivot #3: Bas</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Biz"">
        <h1>Pivot #4: Biz</h1>
        <div>
            In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
            each word has the power to transform into something extraordinary. Here lies the start of something new—an
            opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
            idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
            begins here, in this quiet moment where everything is possible.
        </div>
    </BitPivotItem>
</BitPivot>

<div>Last header clicked: <b>@selectedPivotItem?.HeaderText</b></div>";
    private readonly string example7CsharpCode = @"
private BitPivotItem selectedPivotItem;";

    private readonly string example8RazorCode = @"
<BitPivot>
    <BitPivotItem>
        <Header>
            <span style=""color:red"">Header #1</span>
        </Header>
        <Body>
            <h1>Pivot #1</h1>
            <div>
                Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
                Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
                when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
                for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
            </div>
        </Body>
    </BitPivotItem>
    <BitPivotItem ItemCount=""99"">
        <Header>
            <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
            <span style=""color:blue"">Header #2</span>
            <i style=""color:green"" class=""bit-icon bit-icon--HeartFill""></i>
        </Header>
        <Body>
            <h1>Pivot #2</h1>
            <div>
                Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
                These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
                Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
                inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
                spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
                in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
                are boundless. This space is yours to craft, yours to shape, yours to bring to life.
            </div>
        </Body>
    </BitPivotItem>
    <BitPivotItem IconName=""@BitIconName.Inbox"">
        <Header>
            <span style=""color:rebeccapurple"">Header <i style=""color:purple"" class=""bit-icon bit-icon--HeartFill""></i> #3</span>
        </Header>
        <Body>
            <h1>Pivot #3</h1>
            <div>
                In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
                to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
                possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
                vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
                shaped into meaning, and the emotions ready to resonate with every reader.
            </div>
        </Body>
    </BitPivotItem>
</BitPivot>";

    private readonly string example9RazorCode = @"
<BitPivot Alignment=""BitAlignment.Center"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h2>Pivot #2</h2>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example10RazorCode = @"
<BitPivot Position=""BitPivotPosition.Top"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1: File</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">
        <h1>Pivot #2: Shared</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.Bottom"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1: File</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared"">
        <h1>Pivot #2: Shared</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.Start"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1: File</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" Style=""width:130px"">
        <h1>Pivot #2: Shared with me</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Position=""BitPivotPosition.End"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1: File</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"" Style=""width:130px"">
        <h1>Pivot #2: Shared with me</h1>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h1>Pivot #3: Recent</h1>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example11RazorCode = @"
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

    .custom-body {
        padding: 0.5rem;
        background-color: deepskyblue;
    }
</style>

<BitPivot Style=""border: 1px solid tomato;"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h2>Pivot #2</h2>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Class=""custom-class"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h2>Pivot #2</h2>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Styles=""@(new() { HeaderIcon = ""color: tomato;"", HeaderText = ""color: purple;"" })"">
    <BitPivotItem HeaderText=""File"" IconName=""Info"">
        <h1>Pivot #1</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h2>Pivot #2</h2>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>

<BitPivot Classes=""@(new() { Body = ""custom-body"", SelectedItem = ""custom-selected-item"", Header = ""custom-header"" })"">
    <BitPivotItem HeaderText=""File"">
        <h1>Pivot #1</h1>
        <div>
            Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
            Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment
            when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth,
            for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Shared with me"">
        <h2>Pivot #2</h2>
        <div>
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape.
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought,
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </BitPivotItem>
    <BitPivotItem HeaderText=""Recent"">
        <h3>Pivot #3</h3>
        <div>
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
            shaped into meaning, and the emotions ready to resonate with every reader.
        </div>
    </BitPivotItem>
</BitPivot>";

    private readonly string example12RazorCode = @"
<BitPivot Dir=""BitDir.Rtl"" OverflowBehavior=""@BitPivotOverflowBehavior.Scroll"">
    <BitPivotItem HeaderText=""اسناد"" IconName=""@BitIconName.Info"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و
        برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"" ItemCount=""8"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
        شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"" IconName=""@BitIconName.Info"" ItemCount=""6"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه
        راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای
        اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد
    </BitPivotItem>
</BitPivot>

<BitPivot Dir=""BitDir.Rtl"" Position=""BitPivotPosition.Start"">
    <BitPivotItem HeaderText=""اسناد"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و
        برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و
        برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
        شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
        شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه
        راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای
        اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه
        راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای
        اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </BitPivotItem>
</BitPivot>

<BitPivot Dir=""BitDir.Rtl"" Position=""BitPivotPosition.End"">
    <BitPivotItem HeaderText=""اسناد"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و
        برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و
        برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""آخرین ها"">
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
        شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها
        شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
    </BitPivotItem>
    <BitPivotItem HeaderText=""شخصی"">
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه
        راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای
        اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه
        راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای
        اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </BitPivotItem>
</BitPivot>";
}
