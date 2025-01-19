namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Panel;

public partial class BitPanelDemo
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
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the panel.",
            Href = "#class-styles",
            LinkType = LinkType.Link,
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
            Name = "Styles",
            Type = "BitPanelClassStyles?",
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
            Title = "BitPanelClassStyles",
            Parameters =
            [
               new()
               {
                   Name = "Root",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the root element of the BitPanel."
               },
               new()
               {
                   Name = "Overlay",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the overlay of the BitPanel."
               },
               new()
               {
                   Name = "Container",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the container of the BitPanel."
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

    private bool isBlockingPanelOpen;
    private bool isModelessPanelOpen;
    private BitPanel modelessPanelRef = default!;
    private bool isAutoToggleScrollPanelOpen;

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
<BitPanel @bind-IsOpen=""isBasicPanelOpen"">
    <div style=""max-width:300px;padding:1rem"">
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis.
    </div>
</BitPanel>";
    private readonly string example1CsharpCode = @"
private bool isBasicPanelOpen;";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isBlockingPanelOpen = true"">Open Panel</BitButton>
<BitPanel @bind-IsOpen=""isBlockingPanelOpen"" Blocking>
    <div style=""max-width:300px;padding:1rem"">
        <h3>Blocking</h3>
        <div>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
        <BitButton OnClick=""() => isBlockingPanelOpen = false"">Close</BitButton>
    </div>
</BitPanel>

<BitButton OnClick=""() => isModelessPanelOpen = true"">Open Panel</BitButton>
<BitPanel @bind-IsOpen=""isModelessPanelOpen"" @ref=""modelessPanelRef"" Modeless>
    <div style=""max-width:300px;padding:1rem"">
        <h3>Modeless</h3>
        <div>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non.
        </div>
        <BitButton OnClick=""() => modelessPanelRef.Close()"">Close</BitButton>
    </div>
</BitPanel>

<BitButton OnClick=""() => isAutoToggleScrollPanelOpen = true"">Open Panel</BitButton>
<BitPanel @bind-IsOpen=""isAutoToggleScrollPanelOpen"" AutoToggleScroll>
    <div style=""max-width:300px;padding:1rem"">
        <h3>AutoToggleScroll</h3>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </div>
</BitPanel>";
    private readonly string example2CsharpCode = @"
private bool isBlockingPanelOpen;
private bool isModelessPanelOpen;
private BitPanel modelessPanelRef = default!;
private bool isAutoToggleScrollPanelOpen;";

    private readonly string example3RazorCode = @"
<BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => isOpenInPositionStart = true"">Start</BitButton>
<BitButton OnClick=""() => isOpenPositionEnd = true"">End</BitButton>
<BitButton OnClick=""() => isOpenInPositionTop = true"">Top</BitButton>
<BitButton OnClick=""() => isOpenInPositionBottom = true"">Bottom</BitButton>

<BitPanel @bind-Size=""customPanelSize""
            @bind-IsOpen=""isOpenInPositionStart""
            Position=""BitPanelPosition.Start"">
    <div style=""padding:1rem"">
        BitPanel with Start position and custom Size.
        <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel @bind-Size=""customPanelSize""
            @bind-IsOpen=""isOpenPositionEnd""
            Position=""BitPanelPosition.End"">
    <div style=""padding:1rem"">
        BitPanel with End position and custom Size.
        <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel @bind-Size=""customPanelSize"" @bind-IsOpen=""isOpenInPositionTop"" Position=""BitPanelPosition.Top"">
    <div style=""padding:1rem"">
        BitPanel with Top position and custom Size.
        <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>

<BitPanel @bind-Size=""customPanelSize""
            @bind-IsOpen=""isOpenInPositionBottom""
            Position=""BitPanelPosition.Bottom"">
    <div style=""padding:1rem"">
        BitPanel with Bottom position and custom Size.
        <BitSpinButton @bind-Value=""customPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
    </div>
</BitPanel>";
    private readonly string example3CsharpCode = @"
private double customPanelSize = 300;
private bool isOpenInPositionStart;
private bool isOpenPositionEnd;
private bool isOpenInPositionTop;
private bool isOpenInPositionBottom;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""() => isStyledPanelOpen = true"">Open Styled panel</BitButton>
<BitPanel @bind-IsOpen=""isStyledPanelOpen"" Style=""font-size: 3rem;"">
    <div style=""padding:1rem"">
        BitPanel with custom style.
    </div>
</BitPanel>

<BitButton OnClick=""() => isClassedPanelOpen = true"">Open Classed panel</BitButton>
<BitPanel @bind-IsOpen=""isClassedPanelOpen"" Class=""custom-class"">
    <div style=""padding:1rem"">
        BitPanel with custom class:
        <div class=""item"">Item 1</div>
        <div class=""item"">Item 2</div>
        <div class=""item"">Item 3</div>
    </div>
</BitPanel>

<BitButton OnClick=""() => isPanelStylesOpen = true"">Open panel Styles</BitButton>
<BitPanel @bind-IsOpen=""isPanelStylesOpen""
          Styles=""@(new() { Overlay = ""background-color: #4776f433;"",
                            Container = ""padding: 1rem; box-shadow: 0 0 1rem tomato;"" })"">
    BitPanel with <b>Styles</b> to customize its elements.
</BitPanel>

<BitButton OnClick=""() => isPanelClassesOpen = true"">Open panel Classes</BitButton>
<BitPanel @bind-IsOpen=""isPanelClassesOpen""
          Classes=""@(new() { Container = ""custom-container"",
                             Overlay = ""custom-overlay"" })"">
    BitPanel with <b>Classes</b> to customize its elements.
</BitPanel>";
    private readonly string example4CsharpCode = @"
private bool isStyledPanelOpen;
private bool isClassedPanelOpen;
private bool isPanelStylesOpen;
private bool isPanelClassesOpen;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => isRtlPanelOpenStart = true"">آغاز</BitButton>
<BitButton OnClick=""() => isRtlPanelOpenEnd = true"">پایان</BitButton>

<BitPanel @bind-IsOpen=""isRtlPanelOpenStart""
          Dir=""BitDir.Rtl""
          Position=""BitPanelPosition.Start"">
    <div style=""max-width:300px;padding:1rem"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </div>
</BitPanel
<BitPanel @bind-IsOpen=""isRtlPanelOpenEnd""
          Dir=""BitDir.Rtl""
          Position=""BitPanelPosition.End"">
    <div style=""max-width:300px;padding:1rem"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </div>
</BitPanel>";
    private readonly string example5CsharpCode = @"
private bool isRtlPanelOpenStart;
private bool isRtlPanelOpenEnd;";
}
