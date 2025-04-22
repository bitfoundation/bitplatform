namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Accordion;

public partial class BitAccordionDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Body",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Alias for the ChildContent parameter."
        },
        new()
        {
            Name = "Classes",
            Type = "BitAccordionClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the accordion.",
            LinkType = LinkType.Link,
            Href = "#accordion-class-styles"
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the accordion."
        },
        new()
        {
            Name = "DefaultIsExpanded",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Default value for the IsExpanded parameter."
        },
        new()
        {
            Name = "Description",
            Type = "string?",
            DefaultValue = "null",
            Description = "A short description in the header of the accordion."
        },
        new()
        {
            Name = "HeaderTemplate",
            Type = "RenderFragment<bool>?",
            DefaultValue = "null",
            Description = "Used to customize the header of the accordion."
        },
        new()
        {
            Name = "IsExpanded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Determines whether the accordion is expanded or collapsed."
        },
        new()
        {
            Name = "NoBorder",
            Type = "bool",
            DefaultValue = "false",
            Description = "Removes the default border of the accordion and gives a background color to the body."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            Description = "Callback that is called when the header is clicked."
        },
        new()
        {
            Name = "OnChange",
            Type = "EventCallback<bool>",
            Description = "Callback that is called when the IsExpanded value has changed."
        },
        new()
        {
            Name = "Styles",
            Type = "BitAccordionClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the accordion.",
            LinkType = LinkType.Link,
            Href = "#accordion-class-styles"
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "Title in the header of Accordion."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "accordion-class-styles",
            Title = "BitAccordionClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitAccordion."
                },
                new()
                {
                    Name = "Expanded",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the expanded state of the BitAccordion."
                },
                new()
                {
                    Name = "Header",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header of the BitAccordion."
                },
                new()
                {
                    Name = "HeaderContent",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the header content of the BitAccordion."
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the title of the BitAccordion."
                },
                new()
                {
                    Name = "Description",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the description of the BitAccordion."
                },
                new()
                {
                    Name = "ChevronDownIcon",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the chevron down icon of the BitAccordion."
                },
                new()
                {
                    Name = "ContentContainer",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content container of the BitAccordion."
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the content of the BitAccordion."
                }
            ]
        }
    ];



    private byte controlledAccordionExpandedItem = 1;

    private bool AccordionToggleIsEnabled;
    private bool AccordionToggleIsExpanded;



    private readonly string example1RazorCode = @"
<BitAccordion Title=""Accordion"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>";

    private readonly string example2RazorCode = @"
<BitAccordion Title=""Accordion"" NoBorder>
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>";

    private readonly string example3RazorCode = @"
<BitAccordion Title=""Accordion 1"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
</BitAccordion>
<BitAccordion Title=""Accordion 2"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>
<BitAccordion Title=""Accordion 3"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitAccordion>";

    private readonly string example4RazorCode = @"
<BitAccordion Title=""General settings"" Description=""The general settings of the application"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>";

    private readonly string example5RazorCode = @"
<BitAccordion Title=""General settings""
              Description=""I am an accordion""
              OnClick=""() => controlledAccordionExpandedItem = 1""
              IsExpanded=""controlledAccordionExpandedItem == 1"">
    Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
    Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
    when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
    for ideas that change minds and spark emotions. This is where the journey begins your words will lead the way.
</BitAccordion>
<BitAccordion Title=""Users""
              Description=""You are currently not an owner""
              OnClick=""() => controlledAccordionExpandedItem = 2""
              IsExpanded=""controlledAccordionExpandedItem == 2"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>
<BitAccordion Title=""Advanced settings""
              Description=""Filtering has been entirely disabled for whole web server""
              OnClick=""() => controlledAccordionExpandedItem = 3""
              IsExpanded=""controlledAccordionExpandedItem == 3"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits 
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
    each word has the power to transform into something extraordinary. Here lies the start of something new—an 
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
    begins here, in this quiet moment where everything is possible.
</BitAccordion>";
    private readonly string example5CsharpCode = @"
private byte controlledAccordionExpandedItem = 1;";

    private readonly string example6RazorCode = @"
<BitToggle @bind-Value=""AccordionToggleIsEnabled"" OnText=""Enabled"" OffText=""Disabled"" />
<BitToggle @bind-Value=""AccordionToggleIsExpanded"" OnText=""Expanded"" OffText=""Collapsed"" />

<BitAccordion Title=""Accordion""
              Description=""I am an accordion""
              IsEnabled=""AccordionToggleIsEnabled""
              @bind-IsExpanded=""AccordionToggleIsExpanded"">
    Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
    These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
    Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
    inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
    spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
    in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
    are boundless. This space is yours to craft, yours to shape, yours to bring to life.
</BitAccordion>";
    private readonly string example6CsharpCode = @"
private bool AccordionToggleIsEnabled;
private bool AccordionToggleIsExpanded;";

    private readonly string example7RazorCode = @"
<style>
    .custom-header {
        gap: 1rem;
        flex-grow: 1;
        display: flex;
        line-height: 1.5;
        align-items: center;
    }

    .custom-title {
        color: #0054C6;
    }

    .custom-desc {
        color: brown;
    }
</style>

<BitAccordion>
    <HeaderTemplate Context=""isExpanded"">
        <BitButton Variant=""BitVariant.Text"" IconName=""@(isExpanded ? BitIconName.ChevronDown : BitIconName.ChevronRight)"" />
        <div class=""custom-header"">
            <span class=""custom-title"">Accordion 1</span>
            <span class=""custom-desc"">I am an accordion</span>
        </div>
    </HeaderTemplate>
    <Body>
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
        in potential the quiet magic of beginnings, where everything is still to come, and the possibilities 
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
    </Body>
</BitAccordion>

<BitAccordion Title=""Nature"" Description=""I am an accordion"">
    <BitCarousel AnimationDuration=""1"">
        <BitCarouselItem>
            <img src=""img1.jpg"">
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img2.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img3.jpg"" />
        </BitCarouselItem>
        <BitCarouselItem>
            <img src=""img4.jpg"" />
        </BitCarouselItem>
    </BitCarousel>
</BitAccordion>";

    private readonly string example8RazorCode = @"
<BitAccordion Dir=""BitDir.Rtl"" 
              Title=""تنظیمات"" 
              Description=""من یک آکاردئون هستم!"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
</BitAccordion>";
}
