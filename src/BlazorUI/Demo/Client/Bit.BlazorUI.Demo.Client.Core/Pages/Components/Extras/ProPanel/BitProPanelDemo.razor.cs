namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ProPanel;

public partial class BitProPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "false",
            Description = "Enables the auto scrollbar toggle behavior of the panel.",
        },
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The alias of the ChildContent.",
        },
        new()
        {
            Name = "Blocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the panel can be dismissed by clicking outside of it on the overlay.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the panel.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitProPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Footer",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the footer section of the panel.",
        },
        new()
        {
            Name = "FooterText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the footer section of the panel.",
        },
        new()
        {
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the header section of the panel.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text of the header section of the panel.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines the openness of the panel.",
        },
        new()
        {
            Name = "ModeFull",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the overlay in full mode that gives it an opaque background.",
        },
        new()
        {
            Name = "Modeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the overlay element of the panel.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Panel is dismissed.",
        },
        new()
        {
            Name = "OnSwipeStart",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action starts on the container of the panel.",
        },
        new()
        {
            Name = "OnSwipeMove",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action moves on the container of the panel.",
        },
        new()
        {
            Name = "OnSwipeEnd",
            Type = "EventCallback<decimal>",
            Description = "The event callback for when the swipe action ends on the container of the panel.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPanelPosition?",
            DefaultValue = "null",
            Description = "The position of the panel to show on the screen.",
            Href = "#position-enum",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "Size",
            Type = "double?",
            DefaultValue = "null",
            Description = "The value of the height or width (based on the position) of the Panel.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "null",
            Description = "Specifies the element selector for which the Panel disables its scroll if applicable.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "false",
            Description = "Shows the close button of the Panel.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitProPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the panel component.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "SwipeTrigger",
            Type = "decimal?",
            DefaultValue = "null",
            Description = "The swiping point (difference percentage) based on the width of the panel container to trigger the close action (default is 0.25m).",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitProPanelClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitProPanel."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitProPanel."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitProPanel."
               },
               new()
               {
                   Name = "HeaderContainer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header container of the BitProPanel."
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitProPanel."
               },
               new()
               {
                   Name = "CloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close button of the BitProPanel."
               },
               new()
               {
                   Name = "CloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close icon of the BitProPanel."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the body of the BitProPanel."
               },
               new()
               {
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer container of the BitProPanel."
               }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "position-enum",
            Name = "BitPanelPosition",
            Description = "",
            Items =
            [
                new() { Name = "Start", Value = "0" },
                new() { Name = "End", Value = "1" },
                new() { Name = "Top", Value = "2" },
                new() { Name = "Bottom", Value = "3" }
            ]
        }
    ];



    private bool isBasicProPanelOpen;

    private bool isProPanelWithHeaderTextOpen;
    private bool isProPanelWithHeaderOpen;
    private bool isProPanelWithFooterTextOpen;
    private bool isProPanelWithFooterOpen;

    private bool isBlockingProPanelOpen;
    private bool isModelessProPanelOpen;
    private bool isModeFullProPanelOpen;
    private bool isAutoToggleScrollProPanelOpen;
    private BitProPanel bitProPanelRef = default!;

    private double customProPanelSize = 300;
    private bool isStartProPanelOpen;
    private bool isEndProPanelOpen;
    private bool isTopProPanelOpen;
    private bool isBottomProPanelOpen;

    private bool isStyledProPanelOpen;
    private bool isClassedProPanelOpen;
    private bool isProPanelStylesOpen;
    private bool isProPanelClassesOpen;

    private bool isRtlProPanelOpenStart;
    private bool isRtlProPanelOpenEnd;



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isBasicProPanelOpen = true"">Open ProPanel</BitButton>
<BitProPanel @bind-IsOpen=""isBasicProPanelOpen"">
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicProPanelOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isProPanelWithHeaderTextOpen = true"">Open ProPanel with HeaderText</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelWithHeaderTextOpen"" HeaderText=""BitProPanel with HeaderText"">
    <div style=""max-width:300px"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
        in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities 
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isProPanelWithHeaderOpen = true"">Open ProPanel with Header</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelWithHeaderOpen"">
    <Header>
        <div>
            <div style=""margin-bottom:4px"">BitProPanel with Header</div>
            <BitSearchBox Placeholder=""Search here..."" />
        </div>
    </Header>
    <Body>
        <div style=""max-width:300px"">
            Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
            These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
            Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
            inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
            spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
            in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities 
            are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        </div>
    </Body>
</BitProPanel>


<BitButton OnClick=""() => isProPanelWithFooterTextOpen = true"">Open ProPanel with FooterText</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelWithFooterTextOpen"" FooterText=""BitProPanel with FooterText"">
    <div style=""max-width:300px"">
        In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
        shaped into meaning, and the emotions ready to resonate with every reader.
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
        each word has the power to transform into something extraordinary. Here lies the start of something new—an 
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
        begins here, in this quiet moment where everything is possible.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isProPanelWithFooterOpen = true"">Open ProPanel with Footer</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelWithFooterOpen"">
    <Body>
        <div style=""max-width:300px"">
            In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
            to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
            possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
            vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
            shaped into meaning, and the emotions ready to resonate with every reader.
            In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
            each word has the power to transform into something extraordinary. Here lies the start of something new—an 
            opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
            idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
            begins here, in this quiet moment where everything is possible.
        </div>
    </Body>
    <Footer>
        <h3 style=""margin-bottom:4px"">BitProPanel with Footer</h3>
        <BitButton OnClick=""() => isProPanelWithFooterOpen = false"">Save</BitButton>
        <BitButton OnClick=""() => isProPanelWithFooterOpen = false"" Variant=""BitVariant.Outline"">Close</BitButton>
    </Footer>
</BitProPanel>";
    private readonly string example2CsharpCode = @"
private bool isProPanelWithHeaderTextOpen;
private bool isProPanelWithHeaderOpen;
private bool isProPanelWithFooterTextOpen;
private bool isProPanelWithFooterOpen;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => bitProPanelRef.Open()"">Open ProPanel with ShowCloseButton</BitButton>
<BitProPanel @ref=""bitProPanelRef"" HeaderText=""ShowCloseButton"" ShowCloseButton>
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isBlockingProPanelOpen = true"">Open ProPanel with Blocking</BitButton>
<BitProPanel @bind-IsOpen=""isBlockingProPanelOpen"" HeaderText=""Blocking"" ShowCloseButton Blocking>
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isModelessProPanelOpen = true"">Open ProPanel with Modeless</BitButton>
<BitProPanel @bind-IsOpen=""isModelessProPanelOpen"" HeaderText=""Modeless"" ShowCloseButton Modeless>
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isModeFullProPanelOpen = true"">Open ProPanel with ModeFull</BitButton>
<BitProPanel @bind-IsOpen=""isModeFullProPanelOpen"" HeaderText=""ModeFull"" ShowCloseButton ModeFull>
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isAutoToggleScrollProPanelOpen = true"">Open ProPanel with AutoToggleScroll</BitButton>
<BitProPanel @bind-IsOpen=""isAutoToggleScrollProPanelOpen"" HeaderText=""AutoToggleScroll"" AutoToggleScroll>
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>";
    private readonly string example3CsharpCode = @"
private bool isBlockingProPanelOpen;
private bool isModelessProPanelOpen;
private bool isModeFullProPanelOpen;
private bool isAutoToggleScrollProPanelOpen;
private BitProPanel bitProPanelRef = default!;";

    private readonly string example4RazorCode = @"
<BitNumberField @bind-Value=""customProPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => isStartProPanelOpen = true"">Start</BitButton>
<BitButton OnClick=""() => isEndProPanelOpen = true"">End</BitButton>

<BitButton OnClick=""() => isTopProPanelOpen = true"">Top</BitButton>
<BitButton OnClick=""() => isBottomProPanelOpen = true"">Bottom</BitButton>


<BitProPanel @bind-Size=""customProPanelSize""
             @bind-IsOpen=""isStartProPanelOpen""
             HeaderText=""Start BitProPanel""
             Position=""BitPanelPosition.Start"">
    BitProPanel with Start position and custom Size.
    <BitNumberField @bind-Value=""customProPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
</BitProPanel>

<BitProPanel @bind-Size=""customProPanelSize""
             @bind-IsOpen=""isEndProPanelOpen""
             HeaderText=""End BitProPanel""
             Position=""BitPanelPosition.End"">
    BitProPanel with End position and custom Size.
    <BitNumberField @bind-Value=""customProPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
</BitProPanel>

<BitProPanel @bind-Size=""customProPanelSize""
             @bind-IsOpen=""isTopProPanelOpen""
             HeaderText=""Top BitProPanel""
             Position=""BitPanelPosition.Top"">
    BitProPanel with Top position and custom Size.
    <BitNumberField @bind-Value=""customProPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
</BitProPanel>

<BitProPanel @bind-Size=""customProPanelSize""
             @bind-IsOpen=""isBottomProPanelOpen""
             HeaderText=""Bottom BitProPanel""
             Position=""BitPanelPosition.Bottom"">
    BitProPanel with Bottom position and custom Size.
    <BitNumberField @bind-Value=""customProPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
</BitProPanel>";
    private readonly string example4CsharpCode = @"
private double customPanelSize = 300;
private bool isOpenInPositionStart;
private bool isOpenPositionEnd;
private bool isOpenInPositionTop;
private bool isOpenInPositionBottom;";

    private readonly string example5RazorCode = @"
<style>
    .custom-class .item {
        color: black;
        padding: 1rem;
        margin: 0.5rem;
        border-radius: 0.5rem;
        background-color: brown;
    }

    .custom-container {
        border: 0.25rem solid #0054C6;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-header-container {
        padding: 1.5rem;
        background-color: tomato;
    }

    .custom-header {
        font-size: 2rem;
    }

    .custom-body {
        color: black;
        background-color: lightseagreen;
    }

    .custom-footer {
        color: brown;
        padding: 1.5rem;
        font-size: 1.5rem;
        background-color: tomato;
    }
</style>

<BitButton OnClick=""() => isStyledProPanelOpen = true"">Open Styled ProPanel</BitButton>
<BitProPanel @bind-IsOpen=""isStyledProPanelOpen"" HeaderText=""Style"" ShowCloseButton Style=""font-size: 3rem;"">
    BitProPanel with custom style.
</BitProPanel>

<BitButton OnClick=""() => isClassedProPanelOpen = true"">Open Classed ProPanel</BitButton>
<BitProPanel @bind-IsOpen=""isClassedProPanelOpen"" HeaderText=""Class"" ShowCloseButton Class=""custom-class"">
    BitProPanel with custom class:
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isProPanelStylesOpen = true"">Open ProPanel Styles</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelStylesOpen""
             HeaderText=""Styles"" ShowCloseButton
             Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                               Container = ""box-shadow: 0 0 1rem tomato;"" })"">
    BitProPanel with <b>Styles</b> to customize its elements.
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isProPanelClassesOpen = true"">Open ProPanel Classes</BitButton>
<BitProPanel @bind-IsOpen=""isProPanelClassesOpen""
             HeaderText=""Classes"" ShowCloseButton
             FooterText=""This is a footer text!""
             Classes=""@(new() { Container = ""custom-container"",
                                Overlay = ""custom-overlay"",
                                HeaderContainer = ""custom-header-container"",
                                Header = ""custom-header"",
                                Body = ""custom-body"",
                                Footer = ""custom-footer"" })"">
    BitProPanel with <b>Classes</b> to customize its elements.
    <div style=""max-width:300px"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
    </div>
</BitProPanel>";
    private readonly string example5CsharpCode = @"
private bool isStyledPanelOpen;
private bool isClassedPanelOpen;
private bool isPanelStylesOpen;
private bool isPanelClassesOpen;";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => isRtlPanelOpenStart = true"">آغاز</BitButton>
<BitButton OnClick=""() => isRtlPanelOpenEnd = true"">پایان</BitButton>

<BitProPanel @bind-IsOpen=""isRtlProPanelOpenStart""
             Dir=""BitDir.Rtl""
             HeaderText=""سرصفحه ی آغاز""
             Position=""BitPanelPosition.Start"">
    <div style=""max-width:300px"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </div>
</BitProPanel
<BitProPanel @bind-IsOpen=""isRtlProPanelOpenEnd""
             Dir=""BitDir.Rtl""
             HeaderText=""سرصفحه ی پایان""
             Position=""BitPanelPosition.End"">
    <div style=""max-width:300px"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </div>
</BitProPanel>";
    private readonly string example6CsharpCode = @"
private bool isRtlProPanelOpenStart;
private bool isRtlProPanelOpenEnd;";
}
