namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Swiper;

public partial class BitSwiperDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnimationDuration",
            Type = "double",
            DefaultValue = "0.5",
            Description = "Sets the duration of the scrolling animation in seconds (the default value is 0.5)."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Items of the swiper."
        },
        new()
        {
            Name = "HideNextPrev",
            Type = "bool",
            DefaultValue = "false",
            Description = "Hides the Next/Prev buttons of the BitSwiper."
        },
        new()
        {
            Name = "NextIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display in the next navigation button using custom CSS classes for external icon libraries. Takes precedence over NextIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "NextIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display in the next navigation button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "PrevIcon",
            Type = "BitIconInfo?",
            DefaultValue = "null",
            Description = "Gets or sets the icon to display in the previous navigation button using custom CSS classes for external icon libraries. Takes precedence over PrevIconName when both are set.",
            LinkType = LinkType.Link,
            Href = "#bit-icon-info",
        },
        new()
        {
            Name = "PrevIconName",
            Type = "string?",
            DefaultValue = "null",
            Description = "Gets or sets the name of the icon to display in the previous navigation button from the built-in Fluent UI icons.",
            LinkType = LinkType.Link,
            Href = "https://blazorui.bitplatform.dev/iconography",
        },
        new()
        {
            Name = "ScrollItemsCount",
            Type = "int",
            DefaultValue = "1",
            Description = "Number of items that is going to be changed on navigation."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "bit-icon-info",
            Title = "BitIconInfo",
            Parameters =
            [
               new()
               {
                   Name = "Name",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the name of the icon."
               },
               new()
               {
                   Name = "BaseClass",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the base CSS class for the icon. For built-in Fluent UI icons, this defaults to \"bit-icon\". For external icon libraries like FontAwesome, you might set this to \"fa\" or leave empty."
               },
               new()
               {
                   Name = "Prefix",
                   Type = "string?",
                   DefaultValue = "null",
                   Description = "Gets or sets the CSS class prefix used before the icon name. For built-in Fluent UI icons, this defaults to \"bit-icon--\". For external icon libraries, you might set this to \"fa-\" or leave empty."
               },
            ]
        },
    ];



    private readonly string example1RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper>
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example2RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example3RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper HideNextPrev ScrollItemsCount=""2"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">@index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example4RazorCode = @"
<style>
    .item {
        width: 250px;
        height: 100%;
    }

    .number {
        top: 0.75rem;
        color: #D7D7D7;
        padding: 0.75rem;
        position: absolute;
        font-size: 0.75rem;
        white-space: nowrap;
    }

    .image {
        width: 100%;
        height: 100%;
    }
</style>


<BitSwiper Dir=""BitDir.Rtl"">
    @for (int i = 1; i <= 32; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">مورد @index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";

    private readonly string example5RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitSwiper NextIcon=""@BitIconInfo.Fa(""solid chevron-right"")"" PrevIcon=""@BitIconInfo.Fa(""solid chevron-left"")"">
    @for (int i = 1; i <= 8; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitSwiper NextIcon=""@BitIconInfo.Bi(""arrow-right"")"" PrevIcon=""@BitIconInfo.Bi(""arrow-left"")"">
    @for (int i = 1; i <= 8; i++)
    {
        var index = i;
        var imageIndex = (index - 1) % 4 + 1;
        <BitSwiperItem Class=""item"">
            <div class=""number"">Item @index</div>
            <img class=""image"" src=""img@(imageIndex).jpg"" />
        </BitSwiperItem>
    }
</BitSwiper>";
}
