namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Embed;

public partial class BitEmbedDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Allow",
            Type = "string?",
            DefaultValue = "null",
            Description = "The features the embedded content may use, as the value of the allow attribute of the iframe - its permissions policy (e.g. \"autoplay; encrypted-media; picture-in-picture\").",
        },
        new()
        {
            Name = "AllowFullScreen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lets the embedded content go full screen (the allowfullscreen attribute of the iframe), which a video player needs for its full screen button to work.",
        },
        new()
        {
            Name = "AspectRatio",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aspect ratio of the frame, as a CSS aspect-ratio value (e.g. \"16 / 9\", \"4 / 3\" or \"1\"). Default: 16 / 9, the shape of a video. It wins over the --bit-Embed-aspect-ratio variable.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitEmbedClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the embed.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Loading",
            Type = "BitImageLoading",
            DefaultValue = "BitImageLoading.Lazy",
            Description = "Allows for browser-level loading of the iframe (lazy or eager). A lazy iframe is only loaded once the browser expects it to be scrolled into view.",
            LinkType = LinkType.Link,
            Href = "#image-loading-enum",
        },
        new()
        {
            Name = "MaxWidth",
            Type = "string?",
            DefaultValue = "null",
            Description = "The largest width of the frame, as a CSS length (e.g. \"800px\" or \"50rem\"); below it the frame takes the width of its container. By default it has no limit. It wins over the --bit-Embed-max-width variable.",
        },
        new()
        {
            Name = "PlaceholderAriaLabel",
            Type = "string?",
            DefaultValue = "null",
            Description = "The accessible name of the placeholder button. Defaults to BitStrings.LoadFormat (\"Load {0}\") filled in with the Title.",
        },
        new()
        {
            Name = "PlaceholderTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content shown in the frame in place of the iframe until the user activates it - a thumbnail and a play icon, most of the time. With it the iframe is not rendered, and so nothing of it is requested, until the placeholder is clicked or activated with the keyboard. It is rendered inside a button, so it should hold no interactive content of its own.",
        },
        new()
        {
            Name = "ReferrerPolicy",
            Type = "BitImageReferrerPolicy?",
            DefaultValue = "null",
            Description = "Specifies how much of the address of the current page is sent to the site serving the embedded content (the referrerpolicy attribute of the iframe). By default the browser's own policy applies.",
            LinkType = LinkType.Link,
            Href = "#image-referrer-policy",
        },
        new()
        {
            Name = "Sandbox",
            Type = "string?",
            DefaultValue = "null",
            Description = "Applies extra restrictions to the embedded content, as the value of the sandbox attribute of the iframe: an empty string applies them all, and each token lifts one of them (e.g. \"allow-scripts allow-same-origin\"). By default the iframe is not sandboxed.",
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "The address of the content to embed. Only an http or https address, or a relative one, is rendered; any other scheme (javascript:, data:, blob:, ...) is refused and the frame is left empty.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitEmbedClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the embed.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title of the iframe, which is how a screen reader names the embedded content. An iframe needs one to be accessible, so the parameter is EditorRequired: the compiler warns about a BitEmbed written without it.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitEmbedClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element (the frame) of the BitEmbed." },
                new() { Name = "Placeholder", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the placeholder button of the BitEmbed, which its PlaceholderTemplate is rendered in." },
                new() { Name = "Frame", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the iframe of the BitEmbed." },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        DemoSharedEnums.BitImageLoading(),
        new()
        {
            Id = "image-referrer-policy",
            Name = "BitImageReferrerPolicy",
            Description = "Represents the img and iframe referrerpolicy attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Referrer-Policy",
            Items =
            [
                new()
                {
                    Name= "NoReferrer",
                    Description="Sends no Referer header at all.",
                    Value="0",
                },
                new()
                {
                    Name= "NoReferrerWhenDowngrade",
                    Description="Sends the full URL, except to a less secure destination (HTTPS to HTTP), where nothing is sent.",
                    Value="1",
                },
                new()
                {
                    Name= "Origin",
                    Description="Sends only the origin - the scheme, the host and the port - of the current page.",
                    Value="2",
                },
                new()
                {
                    Name= "OriginWhenCrossOrigin",
                    Description="Sends the full URL to the same origin, and only the origin to any other one.",
                    Value="3",
                },
                new()
                {
                    Name= "SameOrigin",
                    Description="Sends the full URL to the same origin, and nothing at all to any other one.",
                    Value="4",
                },
                new()
                {
                    Name= "StrictOrigin",
                    Description="Sends only the origin, and nothing to a less secure destination (HTTPS to HTTP).",
                    Value="5",
                },
                new()
                {
                    Name= "StrictOriginWhenCrossOrigin",
                    Description="The default behavior: the full URL to the same origin, the origin alone to another secure one, and nothing to a less secure destination (HTTPS to HTTP).",
                    Value="6",
                },
                new()
                {
                    Name= "UnsafeUrl",
                    Description="Sends the full URL to every destination, secure or not.",
                    Value="7",
                }
            ]
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-Embed-aspect-ratio", DefaultValue = "16 / 9", Description = "Shape of the frame. The AspectRatio parameter wins over it." },
        new() { Name = "--bit-Embed-max-width", DefaultValue = "none", Description = "Largest width of the frame. The MaxWidth parameter wins over it." },
        new() { Name = "--bit-Embed-radius", DefaultValue = "0", Description = "Corner radius of the frame, which crops the embedded content." },
        new() { Name = "--bit-Embed-background", DefaultValue = "--bit-clr-bg-sec", Description = "Background of the frame, behind the content while it loads." },
        new() { Name = "--bit-Embed-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Color of the keyboard focus ring of the placeholder." },
    ];
}
