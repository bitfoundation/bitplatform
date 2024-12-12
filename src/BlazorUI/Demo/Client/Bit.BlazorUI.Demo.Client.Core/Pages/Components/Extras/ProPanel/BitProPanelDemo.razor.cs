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
            Name = "Header",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The template used to render the header section of the panel.",
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



    private bool isBasicPanelOpen;

    private bool isPanelWithHeaderOpen;
    private bool isPanelWithFooterOpen;

    private bool isBlockingPanelOpen;
    private bool isModelessPanelOpen;
    private bool isModeFullPanelOpen;
    private bool isAutoToggleScrollPanelOpen;
    private BitProPanel bitPanelRef = default!;

    private double customPanelSize = 300;
    private bool isOpenInPositionStart;
    private bool isOpenPositionEnd;
    private bool isOpenInPositionTop;
    private bool isOpenInPositionBottom;

    private bool isStyledPanelOpen;
    private bool isClassedPanelOpen;
    private bool isPanelStylesOpen;
    private bool isPanelClassesOpen;

    private bool isRtlPanelOpenStart;
    private bool isRtlPanelOpenEnd;



    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => isBasicPanelOpen = true"">Open Panel</BitButton>
<BitProPanel @bind-IsOpen=""isBasicPanelOpen"">
    <div style=""width:300px"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis.
    </div>
</BitProPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicPanelOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isPanelWithHeaderOpen = true"">Open Panel with Header</BitButton>
<BitProPanel @bind-IsOpen=""isPanelWithHeaderOpen"">
    <Header>
        <div>
            <div style=""margin-bottom:4px"">BitPanel with header content</div>
            <BitSearchBox Placeholder=""Search here..."" />
        </div>
    </Header>
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </div>
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isPanelWithFooterOpen = true"">Open Panel with Footer</BitButton>
<BitProPanel @bind-IsOpen=""isPanelWithFooterOpen"">
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </div>
    </Body>
    <Footer>
        <BitButton OnClick=""() => isPanelWithFooterOpen = false"">Save</BitButton>
        <BitButton OnClick=""() => isPanelWithFooterOpen = false"" Variant=""BitVariant.Outline"">Close</BitButton>
    </Footer>
</BitProPanel>";
    private readonly string example2CsharpCode = @"
private bool isPanelWithHeaderOpen;
private bool isPanelWithFooterOpen;";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => bitPanelRef.Open()"">Open Panel</BitButton>
<BitProPanel @ref=""bitPanelRef"" ShowCloseButton>
    <Header>ShowCloseButton</Header>
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isBlockingPanelOpen = true"">Open Panel</BitButton>
<BitProPanel @bind-IsOpen=""isBlockingPanelOpen"" Blocking ShowCloseButton>
    <Header>Blocking</Header>
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isModelessPanelOpen = true"">Open Panel</BitButton>
<BitProPanel @bind-IsOpen=""isModelessPanelOpen"" Modeless ShowCloseButton>
    <Header>Modeless</Header>
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isModeFullPanelOpen = true"">Open Panel</BitButton>
<BitProPanel @bind-IsOpen=""isModeFullPanelOpen"" ModeFull ShowCloseButton>
    <div style=""width:300px"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isAutoToggleScrollPanelOpen = true"">Open Panel</BitButton>
<BitProPanel HeaderText=""AutoToggleScroll"" @bind-IsOpen=""isAutoToggleScrollPanelOpen"" AutoToggleScroll>
    <div style=""width:300px"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </div>
</BitProPanel>

<BitButton OnClick=""() => isAutoToggleScrollPanelOpen = true"">Open Panel</BitButton>
<BitProPanel @bind-IsOpen=""isAutoToggleScrollPanelOpen"" AutoToggleScroll>
    <Header>AutoToggleScroll</Header>
    <Body>
        <div style=""width:300px"">
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
    </Body>
</BitProPanel>";
    private readonly string example3CsharpCode = @"
private bool isBlockingPanelOpen;
private bool isModelessPanelOpen;
private bool isModeFullPanelOpen;
private bool isAutoToggleScrollPanelOpen;
private BitProPanel bitPanelRef = default!;";

    private readonly string example4RazorCode = @"
<BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => isOpenInPositionStart = true"">Start</BitButton>
<BitButton OnClick=""() => isOpenPositionEnd = true"">End</BitButton>
<BitButton OnClick=""() => isOpenInPositionTop = true"">Top</BitButton>
<BitButton OnClick=""() => isOpenInPositionBottom = true"">Bottom</BitButton>

<BitProPanel @bind-Size=""customPanelSize"" @bind-IsOpen=""isOpenInPositionStart"" Position=""BitPanelPosition.Start"">
    BitPanel with Start position and custom Size.
    <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
</BitProPanel>

<BitProPanel @bind-Size=""customPanelSize"" @bind-IsOpen=""isOpenPositionEnd"" Position=""BitPanelPosition.End"">
    BitPanel with End position and custom Size.
    <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
</BitProPanel>

<BitProPanel @bind-Size=""customPanelSize"" @bind-IsOpen=""isOpenInPositionTop"" Position=""BitPanelPosition.Top"">
    BitPanel with Top position and custom Size.
    <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
</BitProPanel>

<BitProPanel @bind-Size=""customPanelSize"" @bind-IsOpen=""isOpenInPositionBottom"" Position=""BitPanelPosition.Bottom"">
    BitPanel with Bottom position and custom Size.
    <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
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
        width: 3rem;
        height: 3rem;
        margin: 0.5rem;
        border-radius: 0.5rem;
        background-color: brown;
    }

    .custom-container {
        border: 0.25rem solid #0054C6;
        border-end-start-radius: 1rem;
        border-start-start-radius: 1rem;
    }

    .custom-overlay {
        background-color: #ffbd5a66;
    }

    .custom-header {
        padding-bottom: 1.25rem;
        background-color: tomato;
    }

    .custom-body {
        padding-top: 1.25rem;
        background-color: lightseagreen;
    }
</style>

<BitButton OnClick=""() => isStyledPanelOpen = true"">Open Styled panel</BitButton>
<BitProPanel @bind-IsOpen=""isStyledPanelOpen"" Style=""font-size: 3rem;"">
    <Header>Style</Header>
    <Body>
        BitPanel with custom style.
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isClassedPanelOpen = true"">Open Classed panel</BitButton>
<BitProPanel @bind-IsOpen=""isClassedPanelOpen"" Class=""custom-class"">
    <Header>Class</Header>
    <Body>
        BitPanel with custom class:
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isPanelStylesOpen = true"">Open panel Styles</BitButton>
<BitProPanel @bind-IsOpen=""isPanelStylesOpen""
             Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                               Container = ""box-shadow: 0 0 1rem tomato;"" })"">
    <Header>Styles</Header>
    <Body>
        BitPanel with
        <b>Styles</b> to customize its elements.
    </Body>
</BitProPanel>

<BitButton OnClick=""() => isPanelClassesOpen = true"">Open panel Classes</BitButton>
<BitProPanel @bind-IsOpen=""isPanelClassesOpen""
             Classes=""@(new() { Container = ""custom-container"",
                                Overlay = ""custom-overlay"",
                                Body = ""custom-body"",
                                Header = ""custom-header"" })"">
    <Header>Classes</Header>
    <Body>
        BitPanel with
        <b>Classes</b> to customize its elements.
    </Body>
</BitProPanel>";
    private readonly string example5CsharpCode = @"
private bool isStyledPanelOpen;
private bool isClassedPanelOpen;
private bool isPanelStylesOpen;
private bool isPanelClassesOpen;";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => isRtlPanelOpenStart = true"">آغاز</BitButton>
<BitButton OnClick=""() => isRtlPanelOpenEnd = true"">پایان</BitButton>

<BitProPanel @bind-IsOpen=""isRtlPanelOpenStart"" Dir=""BitDir.Rtl"" Position=""BitPanelPosition.Start"">
    <Header>سرصفحه ی آغاز</Header>
    <Body>
        <div style=""width:300px"">
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </div>
    </Body>
</BitProPanel>

<BitProPanel @bind-IsOpen=""isRtlPanelOpenEnd"" Dir=""BitDir.Rtl"" Position=""BitPanelPosition.End"">
    <Header>سرصفحه ی پایان</Header>
    <Body>
        <div style=""width:300px"">
            لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
            چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
            کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
            در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
        </div>
    </Body>
</BitProPanel>";
    private readonly string example6CsharpCode = @"
private bool isRtlPanelOpenStart;
private bool isRtlPanelOpenEnd;";
}
