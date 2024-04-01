namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Panel;

public partial class BitPanelDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "AutoToggleScroll",
            Type = "bool",
            DefaultValue = "true",
            Description = "Enables the auto scrollbar toggle behavior of the Panel.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of Panel, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitPanel component.",
            Href = "#panel-class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "FooterTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the footer inside the Panel is rendered.",
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Used to customize how the header inside the Panel is rendered.",
        },
        new()
        {
            Name = "HeaderText",
            Type = "string?",
            DefaultValue = "null",
            Description = "Header text of Panel.",
        },
        new()
        {
            Name = "IsBlocking",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog can be light dismissed by clicking outside the dialog (on the overlay).",
        },
        new()
        {
            Name = "IsModeless",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog should be modeless (e.g. not dismiss when focusing/clicking outside of the dialog). if true: IsBlocking is ignored, there will be no overlay.",
        },
        new()
        {
            Name = "IsOpen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the dialog is displayed.",
        },
        new()
        {
            Name = "OnDismiss",
            Type = "EventCallback<MouseEventArgs>",
            Description = "A callback function for when the Panel is dismissed.",
        },
        new()
        {
            Name = "Position",
            Type = "BitPanelPosition",
            LinkType = LinkType.Link,
            Href = "#component-position-enum",
            DefaultValue = "BitPanelPosition.Right",
            Description = "Position of the modal on the screen.",
        },
        new()
        {
            Name = "Size",
            Type = "double",
            DefaultValue = "0",
            Description = "Provides Height or Width for the Panel.",
        },
        new()
        {
            Name = "ScrollerSelector",
            Type = "string",
            DefaultValue = "body",
            Description = "Set the element selector for which the Panel disables its scroll if applicable.",
        },
        new()
        {
            Name = "ShowCloseButton",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows or hides the close button of the Panel.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitPanelClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitPanel component.",
            Href = "#panel-class-styles",
            LinkType = LinkType.Link,
        },
        new()
        {
            Name = "SubtitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the subtitle of the Panel, if any.",
        },
        new()
        {
            Name = "TitleAriaId",
            Type = "string?",
            DefaultValue = "null",
            Description = "ARIA id for the title of the Panel, if any.",
        },
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "panel-class-styles",
            Title = "BitPanelClassStyles",
            Parameters = new()
            {
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
               },
               new()
               {
                   Name = "Header",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header of the BitPanel."
               },
               new()
               {
                   Name = "HeaderText",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the header text of the BitPanel."
               },
               new()
               {
                   Name = "CloseButton",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close button of the BitPanel."
               },
               new()
               {
                   Name = "CloseIcon",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the close icon of the BitPanel."
               },
               new()
               {
                   Name = "Body",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the body of the BitPanel."
               },
               new()
               {
                   Name = "Footer",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Custom CSS classes/styles for the footer of the BitPanel."
               }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "component-position-enum",
            Name = "BitPanelPosition",
            Description = "",
            Items = new List<ComponentEnumItem>()
            {
                new() { Name = "Right", Value = "0" },
                new() { Name = "Left", Value = "1" },
                new() { Name = "Top", Value = "2" },
                new() { Name = "Bottom", Value = "3" }
            }
        }
    };



    private readonly string example1RazorCode = @"
<BitButton OnClick=@(() => IsBasicPanelOpen = true)>Open Panel</BitButton>

<BitPanel @bind-IsOpen=""IsBasicPanelOpen"">
    Content goes here.
</BitPanel>";
    private readonly string example1CsharpCode = @"
private bool IsBasicPanelOpen = false;";

    private readonly string example2RazorCode = @"
<BitLabel>Panel with header text</BitLabel>
<BitButton OnClick=@(() => IsPanelWithHeaderTextOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with custom header content</BitLabel>
<BitButton OnClick=@(() => IsPanelWithCustomHeaderOpen = true)>Open Panel</BitButton>

<BitPanel HeaderText=""Simple header"" @bind-IsOpen=""IsPanelWithHeaderTextOpen"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
        turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
        ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
        Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
        Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
        efficitur.
    </p>
</BitPanel>

<BitPanel @bind-IsOpen=""IsPanelWithCustomHeaderOpen"">
    <HeaderTemplate>
        <div>
            <BitSearchBox Placeholder=""Search here..."" />
            <p>
                BitPanel with custom header content
            </p>
        </div>
    </HeaderTemplate>
    <ChildContent>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </ChildContent>
</BitPanel>";
    private readonly string example2CsharpCode = @"
private bool IsPanelWithHeaderTextOpen = false;
private bool IsPanelWithCustomHeaderOpen = false;";

    private readonly string example3RazorCode = @"
<BitLabel>Panel with custom footer content</BitLabel>
<BitButton OnClick=@(() => IsPanelWithFooterOpen = true)>Open Panel</BitButton>

<BitPanel Title=""BitPanel with custom footer content"" @bind-IsOpen=""IsPanelWithFooterOpen"">
    <ChildContent>
        <p>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
            amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
            sagittis nunc, ut interdum ipsum vestibulum non. Proin dolor elit, aliquam eget tincidunt non, vestibulum ut
            turpis. In hac habitasse platea dictumst. In a odio eget enim porttitor maximus. Aliquam nulla nibh,
            ullamcorper aliquam placerat eu, viverra et dui. Phasellus ex lectus, maximus in mollis ac, luctus vel eros.
            Vivamus ultrices, turpis sed malesuada gravida, eros ipsum venenatis elit, et volutpat eros dui et ante.
            Quisque ultricies mi nec leo ultricies mollis. Vivamus egestas volutpat lacinia. Quisque pharetra eleifend
            efficitur.
        </p>
    </ChildContent>
    <FooterTemplate>
        <BitButton OnClick=@(() => IsPanelWithFooterOpen = false)>Save</BitButton>
        <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=@(() => IsPanelWithFooterOpen = false)>Close</BitButton>
    </FooterTemplate>
</BitPanel>";
    private readonly string example3CsharpCode = @"
private bool IsPanelWithFooterOpen = false;";
    
    private readonly string example4RazorCode = @"
<BitLabel>Panel with IsBlocking = true</BitLabel>
<BitButton OnClick=@(() => IsBlockingPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with IsModeless = true</BitLabel>
<BitButton OnClick=@(() => IsModelessPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with AutoToggleScroll = false</BitLabel>
<BitButton OnClick=@(() => IsAutoToggleScrollPanelOpen = true)>Open Panel</BitButton>

<BitLabel>Panel with ShowCloseButton = false</BitLabel>
<BitButton OnClick=""() => bitPanelRef.Open()"">Open Panel</BitButton>

<BitPanel HeaderText=""IsBlocking = true"" @bind-IsOpen=""IsBlockingPanelOpen"" IsBlocking=""true"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel HeaderText=""IsModeless = true"" @bind-IsOpen=""IsModelessPanelOpen"" IsModeless=""true"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel HeaderText=""AutoToggleScroll = false"" @bind-IsOpen=""IsAutoToggleScrollPanelOpen"" AutoToggleScroll=""false"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
</BitPanel>
<BitPanel @ref=""bitPanelRef"" HeaderText=""ShowCloseButton = false"" ShowCloseButton=""false"">
    <p>
        Lorem ipsum dolor sit amet, consectetur adipiscing elit. Maecenas lorem nulla, malesuada ut sagittis sit
        amet, vulputate in leo. Maecenas vulputate congue sapien eu tincidunt. Etiam eu sem turpis. Fusce tempor
        sagittis nunc, ut interdum ipsum vestibulum non.
    </p>
    <BitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""() => bitPanelRef.Close()"">Close</BitButton>
</BitPanel>";
    private readonly string example4CsharpCode = @"
private bool IsBlockingPanelOpen = false;
private bool IsModelessPanelOpen = false;
private bool IsAutoToggleScrollPanelOpen = false;

private BitPanel bitPanelRef = default!;";

    private readonly string example5RazorCode = @"
<BitSpinButton @bind-Value=""CustomPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />

<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Left)"">Left</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Right)"">Right</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Top)"">Top</BitButton>
<BitButton OnClick=""() => OpenPanelInPosition(BitPanelPosition.Bottom)"">Bottom</BitButton>

<BitPanel @bind-Size=""CustomPanelSize"" Title=""Panel types"" @bind-IsOpen=""IsOpenInPosition"" Position=""position"">
    <p>
        BitPanel with custom position and size. Lorem ipsum dolor sit amet, consectetur adipiscing elit.
    </p>
    <BitSpinButton @bind-Value=""CustomPanelSize"" Mode=""BitSpinButtonMode.Inline"" Label=""Custom size"" />
</BitPanel>";
    private readonly string example5CsharpCode = @"
private bool IsOpenInPosition = false;
private double CustomPanelSize = 320;
private BitPanelPosition position;

private void OpenPanelInPosition(BitPanelPosition positionValue)
{
    IsOpenInPosition = true;
    position = positionValue;
}";

    private readonly string example6RazorCode = @"
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


<BitButton OnClick=""() => IsStyledPanelOpen = true"">Open styled panel</BitButton>
<BitButton OnClick=""() => IsClassedPanelOpen = true"">Open classed panel</BitButton>
<BitButton OnClick=""() => IsPanelStylesOpen = true"">Open panel styles</BitButton>
<BitButton OnClick=""() => IsPanelClassesOpen = true"">Open panel classes</BitButton>

<BitPanel @bind-IsOpen=""IsStyledPanelOpen"" Style=""font-size: 3rem;"">
    Content goes here.
    <div class=""item"">Item 1</div>
    <div class=""item"">Item 2</div>
    <div class=""item"">Item 3</div>
</BitPanel>

<BitPanel @bind-IsOpen=""IsClassedPanelOpen"" Class=""custom-class"">
    Content goes here.
</BitPanel>

<BitPanel @bind-IsOpen=""IsPanelStylesOpen"" Styles=""@(new() { Overlay = ""background-color: #4776f433;"", Container = ""box-shadow: 0 0 1rem tomato;"" })"">
    Content goes here.
</BitPanel>

<BitPanel @bind-IsOpen=""IsPanelClassesOpen"" 
          HeaderText=""Simple header""
          Classes=""@(new() { Container = ""custom-container"",
                             Overlay = ""custom-overlay"",
                             Body = ""custom-body"",
                             Header = ""custom-header"" })"">
    Content goes here.
</BitPanel>
";
    private readonly string example6CsharpCode = @"
private bool IsStyledPanelOpen = false;
private bool IsClassedPanelOpen = false;
private bool IsPanelStylesOpen = false;
private bool IsPanelClassesOpen = false;";

    private readonly string example7RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" OnClick=@(() => IsRtlPanelOpen = true)>
    باز کردن پنل
</BitButton>

<BitPanel @bind-IsOpen=""IsRtlPanelOpen""
          Dir=""BitDir.Rtl""
          HeaderText=""سرصفحه ی ساده"">
    <p>
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
        کتابهای زیادی در شصت و سه درصد گذشته، حال و آینده شناخت فراوان جامعه و متخصصان را می طلبد تا با نرم افزارها شناخت بیشتری را برای طراحان رایانه ای علی الخصوص طراحان خلاقی و فرهنگ پیشرو در زبان فارسی ایجاد کرد.
        در این صورت می توان امید داشت که تمام و دشواری موجود در ارائه راهکارها و شرایط سخت تایپ به پایان رسد وزمان مورد نیاز شامل حروفچینی دستاوردهای اصلی و جوابگوی سوالات پیوسته اهل دنیای موجود طراحی اساسا مورد استفاده قرار گیرد.
    </p>
</BitPanel>";
    private readonly string example7CsharpCode = @"
private bool IsRtlPanelOpen = false;";
}
