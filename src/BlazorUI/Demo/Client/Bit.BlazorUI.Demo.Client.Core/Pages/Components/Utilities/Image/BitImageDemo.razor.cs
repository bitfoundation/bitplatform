namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Image;

public partial class BitImageDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alt",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies an alternate text for the image. The attribute is always rendered, so an image given no text is announced as decorative (alt=\"\") rather than read out as a file name."
        },
        new()
        {
            Name = "AspectRatio",
            Type = "string?",
            DefaultValue = "null",
            Description = "The aspect ratio of the frame of the image, as a CSS aspect-ratio value (e.g. \"16/9\" or \"1\"). Reserves the room the image will need before it arrives."
        },
        new()
        {
            Name = "Bordered",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a border around the frame of the image."
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content rendered over the image, filling the frame."
        },
        new()
        {
            Name = "Circular",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the frame of the image as a circle. Takes precedence over Rounded."
        },
        new()
        {
            Name = "Classes",
            Type = "BitImageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the BitImage.",
            LinkType = LinkType.Link,
            Href = "#image-class-style"
        },
        new()
        {
            Name = "Cover",
            Type = "BitImageCover?",
            DefaultValue = "null",
            Description = "Specifies the cover style to be used for this image. Only the CenterCover and CenterContain fits read it.",
            LinkType = LinkType.Link,
            Href = "#image-cover-style"
        },
        new()
        {
            Name = "CrossOrigin",
            Type = "BitImageCrossOrigin?",
            DefaultValue = "null",
            Description = "Specifies the CORS setting the image is requested with.",
            LinkType = LinkType.Link,
            Href = "#image-cross-origin"
        },
        new()
        {
            Name = "Decoding",
            Type = "BitImageDecoding?",
            DefaultValue = "null",
            Description = "Hints the browser at whether the image may be decoded asynchronously.",
            LinkType = LinkType.Link,
            Href = "#image-decoding"
        },
        new()
        {
            Name = "Draggable",
            Type = "bool?",
            DefaultValue = "null",
            Description = "Specifies whether the image can be dragged by the user."
        },
        new()
        {
            Name = "ErrorTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to show the error state of the image.",
        },
        new()
        {
            Name = "FadeIn",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, fades the image in when it becomes visible."
        },
        new()
        {
            Name = "FallbackSrc",
            Type = "string?",
            DefaultValue = "null",
            Description = "The source of the image to show when the one given by Src fails to load, or when no Src is given at all. It is tried exactly once."
        },
        new()
        {
            Name = "FetchPriority",
            Type = "BitImageFetchPriority?",
            DefaultValue = "null",
            Description = "Hints the browser at the priority this image is fetched with, relative to the other resources of the page.",
            LinkType = LinkType.Link,
            Href = "#image-fetch-priority"
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The image height value. A bare number is read as a pixel count; anything else is used as written."
        },
        new()
        {
            Name = "ImageAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional attributes in addition to the image's parameters. The dictionary is merged with the attributes the component builds itself rather than replaced by them."
        },
        new()
        {
            Name = "ImageFit",
            Type = "BitImageFit?",
            DefaultValue = "null",
            Description = "Used to determine how the image is scaled and cropped to fit the frame.",
            LinkType = LinkType.Link,
            Href = "#image-fit"
        },
        new()
        {
            Name = "ImagePosition",
            Type = "string?",
            DefaultValue = "null",
            Description = "The position of the image inside its frame, as a CSS object-position value (e.g. \"top\", \"50% 25%\"). It decides which part of the image survives a crop."
        },
        new()
        {
            Name = "Loading",
            Type = "BitImageLoading?",
            DefaultValue = "null",
            Description = "Allows for browser-level image loading (lazy or eager).",
            LinkType = LinkType.Link,
            Href = "#image-loading"
        },
        new()
        {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom template used to show the loading state of the image.",
        },
        new()
        {
            Name = "MaximizeFrame",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the image frame will expand to fill its parent container."
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<MouseEventArgs>",
            DefaultValue = "null",
            Description = "Callback for when the image is clicked. Assigning it makes the image a focusable button that also answers the Enter and Space keys."
        },
        new()
        {
            Name = "OnError",
            Type = "EventCallback",
            DefaultValue = "null",
            Description = "Callback for when the image fails to load, including the failure that is answered by falling back to the FallbackSrc."
        },
        new()
        {
            Name = "OnLoad",
            Type = "EventCallback",
            DefaultValue = "null",
            Description = "Callback for when the image has been loaded successfully."
        },
        new()
        {
            Name = "OnLoadingStateChange",
            Type = "EventCallback<BitImageState>",
            DefaultValue = "null",
            Description = "Optional callback method for when the image load state has changed.",
            LinkType = LinkType.Link,
            Href = "#image-state"
        },
        new()
        {
            Name = "PlaceholderSrc",
            Type = "string?",
            DefaultValue = "null",
            Description = "The source of a placeholder image shown, blurred, while the image itself is still loading."
        },
        new()
        {
            Name = "ReferrerPolicy",
            Type = "BitImageReferrerPolicy?",
            DefaultValue = "null",
            Description = "Specifies how much of the address of the current page is sent to whoever serves the image.",
            LinkType = LinkType.Link,
            Href = "#image-referrer-policy"
        },
        new()
        {
            Name = "Rounded",
            Type = "bool",
            DefaultValue = "false",
            Description = "Rounds the corners of the frame of the image."
        },
        new()
        {
            Name = "Shadow",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders a shadow under the frame of the image, lifting it off the surface it sits on."
        },
        new()
        {
            Name = "Sizes",
            Type = "string?",
            DefaultValue = "null",
            Description = "The value of the sizes attribute of the image, which tells the browser how wide the image will be laid out at before it knows the layout."
        },
        new()
        {
            Name = "Sources",
            Type = "IEnumerable<BitImageSource>?",
            DefaultValue = "null",
            Description = "The alternative sources of the image, offered to the browser ahead of Src. This is the art-direction and the format-negotiation half of responsive images, which Srcset cannot express.",
            LinkType = LinkType.Link,
            Href = "#image-source"
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the src of the image. Changing it returns the component to the Loading state."
        },
        new()
        {
            Name = "Srcset",
            Type = "string?",
            DefaultValue = "null",
            Description = "The set of image sources the browser may choose from, with their width or density descriptors (e.g. \"photo-480.jpg 480w, photo-960.jpg 960w\")."
        },
        new()
        {
            Name = "StartVisible",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded."
        },
        new()
        {
            Name = "Styles",
            Type = "BitImageClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the BitImage.",
            LinkType = LinkType.Link,
            Href = "#image-class-style"
        },
        new()
        {
            Name = "Title",
            Type = "string?",
            DefaultValue = "null",
            Description = "The title to show when the mouse is placed on the image."
        },
        new()
        {
            Name = "Width",
            Type = "string?",
            DefaultValue = "null",
            Description = "The image width value. A bare number is read as a pixel count; anything else is used as written."
        }
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "FocusAsync",
            Type = "ValueTask",
            DefaultValue = "",
            Description = "Gives the browser focus to the img element of the component. Only a clickable image (one with an OnClick) or one given an explicit TabIndex is focusable at all, so anywhere else the call does nothing."
        },
        new()
        {
            Name = "ImageElement",
            Type = "ElementReference",
            DefaultValue = "",
            Description = "The reference to the img element of the component, for whatever has to reach the picture itself rather than the frame around it. RootElement is that frame."
        },
        new()
        {
            Name = "LoadingState",
            Type = "BitImageState",
            DefaultValue = "BitImageState.Loading",
            Description = "The current loading state of the image.",
            LinkType = LinkType.Link,
            Href = "#image-state"
        },
        new()
        {
            Name = "ReloadAsync",
            Type = "Task",
            DefaultValue = "",
            Description = "Requests the image again from the beginning, whichever state it is in: the component returns to the Loading state, forgets that a FallbackSrc has been tried, and replaces the img element rather than patching it - which is what makes the browser fetch a source it already holds an answer for."
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "image-class-style",
            Title = "BitImageClassStyles",
            Description = "",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the image.",
                },
                new()
                {
                    Name = "Placeholder",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the placeholder image element, which is only rendered while a PlaceholderSrc is provided and the image itself has not loaded yet.",
                },
                new()
                {
                    Name= "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description="Custom CSS classes/styles for the image element."
                },
                new()
                {
                    Name = "LoadingTemplate",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element wrapping the LoadingTemplate of the image.",
                },
                new()
                {
                    Name = "ErrorTemplate",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the element wrapping the ErrorTemplate of the image.",
                },
                new()
                {
                    Name = "Content",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the overlay element that holds the ChildContent of the image.",
                }
            ]
        },
        new()
        {
            Id = "image-source",
            Title = "BitImageSource",
            Description = "One alternative source of the image, rendered as a source element of the picture the image is then wrapped in. The browser walks the sources in order and takes the first one whose Media and Type it is satisfied by.",
            Parameters =
            [
                new()
                {
                    Name = "Srcset",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The set of images this source offers, with their width or density descriptors (e.g. \"photo-480.avif 480w, photo-960.avif 960w\"). This is the only required member.",
                },
                new()
                {
                    Name = "Media",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The media query the source applies to (e.g. \"(max-width: 600px)\"). A source with none applies whatever the viewport is, so it belongs last among the sources that carry one.",
                },
                new()
                {
                    Name = "Sizes",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "How wide the image will be laid out at, for the browser to choose among a width-descriptor Srcset with.",
                },
                new()
                {
                    Name = "Type",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The MIME type of the images this source offers (e.g. \"image/avif\"). A browser that cannot read the type skips the source without fetching anything.",
                },
                new()
                {
                    Name = "Width",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The intrinsic width, in pixels, of the images this source offers.",
                },
                new()
                {
                    Name = "Height",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The intrinsic height, in pixels, of the images this source offers.",
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "image-fit",
            Name = "BitImageFit",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "None",
                    Description="Neither the image nor the frame are scaled. The image keeps its natural size and whatever of it does not fit the frame is cropped away from the right and the bottom.",
                    Value="0",
                },
                new()
                {
                    Name= "Center",
                    Description="The image is not scaled. The image is centered and cropped within the content box.",
                    Value="1",
                },
                new()
                {
                    Name= "CenterContain",
                    Description="The image will be centered horizontally and vertically within the frame and maintains its aspect ratio, scaled down where needed so that all of it fits inside the frame.",
                    Value="2",
                },
                new()
                {
                    Name= "CenterCover",
                    Description="The image will be centered horizontally and vertically within the frame and maintains its aspect ratio, scaled up where needed so that it covers the frame and the overflow is cropped.",
                    Value="3",
                },
                new()
                {
                    Name= "Contain",
                    Description="The image is scaled to maintain its aspect ratio while being fully contained within the frame.",
                    Value="4",
                },
                new()
                {
                    Name= "Cover",
                    Description="The image is scaled to maintain its aspect ratio while filling the frame.",
                    Value="5",
                },
                new()
                {
                    Name= "Fill",
                    Description="The image is stretched to fill the frame exactly, without maintaining its aspect ratio.",
                    Value="6",
                },
                new()
                {
                    Name= "ScaleDown",
                    Description="The image is contained within the frame, but never scaled up: an image smaller than the frame keeps its natural size.",
                    Value="7",
                }
            ]
        },
        new()
        {
            Id = "image-cover-style",
            Name = "BitImageCover",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Landscape",
                    Description="The image will be shown at 100% height of container and the width will be scaled accordingly.",
                    Value="0",
                },
                new()
                {
                    Name= "Portrait",
                    Description="The image will be shown at 100% width of container and the height will be scaled accordingly.",
                    Value="1",
                }
            ]
        },
        new()
        {
            Id = "image-state",
            Name = "BitImageState",
            Description = "",
            Items =
            [
                new()
                {
                    Name= "Loading",
                    Description="The image is loading from its source.",
                    Value="0",
                },
                new()
                {
                    Name= "Loaded",
                    Description="The image has been loaded successfully.",
                    Value="1",
                },
                new()
                {
                    Name= "Error",
                    Description="An error has been encountered while loading the image. Where a FallbackSrc is provided, this state is only reached once that one has failed as well.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "image-loading",
            Name = "BitImageLoading",
            Description = "Represents the img loading attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/loading",
            Items =
            [
                new()
                {
                    Name= "Eager",
                    Description="The default behavior, eager tells the browser to load the image as soon as the img element is processed.",
                    Value="0",
                },
                new()
                {
                    Name= "Lazy",
                    Description="Tells the user agent to hold off on loading the image until the browser estimates that it will be needed imminently.",
                    Value="1",
                }
            ]
        },
        new()
        {
            Id = "image-decoding",
            Name = "BitImageDecoding",
            Description = "Represents the img decoding attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/decoding",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="The default behavior, which leaves the decision to the browser.",
                    Value="0",
                },
                new()
                {
                    Name= "Sync",
                    Description="Decodes the image synchronously, so it is presented together with the rest of the content rendered in the same frame.",
                    Value="1",
                },
                new()
                {
                    Name= "Async",
                    Description="Decodes the image asynchronously, so the rest of the content is not held back while the decoding runs.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "image-fetch-priority",
            Name = "BitImageFetchPriority",
            Description = "Represents the img fetchpriority attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/API/HTMLImageElement/fetchPriority",
            Items =
            [
                new()
                {
                    Name= "Auto",
                    Description="The default behavior, which leaves the priority to the browser's own heuristics.",
                    Value="0",
                },
                new()
                {
                    Name= "High",
                    Description="Fetches the image ahead of the other images of the page, for the one that is the page's largest contentful paint.",
                    Value="1",
                },
                new()
                {
                    Name= "Low",
                    Description="Fetches the image after the other images of the page, for the ones that carry no meaning on the first screen.",
                    Value="2",
                }
            ]
        },
        new()
        {
            Id = "image-cross-origin",
            Name = "BitImageCrossOrigin",
            Description = "Represents the img crossorigin attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/img#crossorigin",
            Items =
            [
                new()
                {
                    Name= "Anonymous",
                    Description="Sends a cross-origin request with no credentials: no cookie, no client certificate and no HTTP authentication.",
                    Value="0",
                },
                new()
                {
                    Name= "UseCredentials",
                    Description="Sends a cross-origin request with credentials. The other origin has to answer with the matching Access-Control-Allow-Credentials header.",
                    Value="1",
                }
            ]
        },
        new()
        {
            Id = "image-referrer-policy",
            Name = "BitImageReferrerPolicy",
            Description = "Represents the img referrerpolicy attribute values explained here: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Referrer-Policy",
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
                    Description="The default behavior: the full URL to the same origin, the origin alone to another secure one, and nothing to a less secure destination.",
                    Value="6",
                },
                new()
                {
                    Name= "UnsafeUrl",
                    Description="Sends the full URL to every destination, secure or not.",
                    Value="7",
                }
            ]
        }
    ];



    private bool loadLoading;
    private bool loadError;
    private bool loadPlaceholder;
    private int fadeKey;
    private int clickCount;
    private string loadingStateText = "Loading";
    private BitImage? slowImage;
    private BitImage? brokenImage;

    // A 16x9 gradient inlined as an SVG data URI: the stand-in for the tiny, heavily compressed copy of
    // the real photograph a page would normally generate at build time.
    private const string placeholderDataUri = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 9'%3E%3Cdefs%3E%3ClinearGradient id='g' x1='0' y1='0' x2='1' y2='1'%3E%3Cstop offset='0' stop-color='%23335c81'/%3E%3Cstop offset='1' stop-color='%23c9d6df'/%3E%3C/linearGradient%3E%3C/defs%3E%3Crect width='16' height='9' fill='url(%23g)'/%3E%3C/svg%3E";

    private async Task ReloadImages()
    {
        if (slowImage is not null)
        {
            await slowImage.ReloadAsync();
        }

        if (brokenImage is not null)
        {
            await brokenImage.ReloadAsync();
        }
    }



    private readonly string example1RazorCode = @"
<BitImage Alt=""The bit platform logo""
          Title=""The bit platform logo""
          Src=""images/bit-logo-blue.png"" />

<BitImage Alt=""The bit platform logo"" IsEnabled=""false"" Src=""images/bit-logo-blue.png"" />";

    private readonly string example2RazorCode = @"
<BitImage Width=""9rem""
          Alt=""The bit platform logo""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""5rem""
          Alt=""The bit platform logo""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Width=""256px""
          Height=""128px""
          Alt=""The bit platform logo""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example3RazorCode = @"
<BitImage Width=""16rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph""
          Class=""framed""
          Src=""images/carousel/img1.jpg"" />

<BitImage Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph""
          Class=""framed""
          Src=""images/carousel/img1.jpg"" />

<BitImage Width=""8rem""
          AspectRatio=""3/4""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph""
          Class=""framed""
          Src=""images/carousel/img1.jpg"" />";

    private readonly string example4RazorCode = @"
<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.None""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.Center""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.Contain""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.Cover""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.Fill""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.ScaleDown""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.CenterContain""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          ImageFit=""BitImageFit.CenterCover""
          Class=""framed""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example5RazorCode = @"
<BitImage Width=""10rem""
          AspectRatio=""1""
          ImagePosition=""top""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph cropped to its top edge""
          Class=""framed""
          Src=""images/carousel/img2.jpg"" />

<BitImage Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph cropped around its middle""
          Class=""framed""
          Src=""images/carousel/img2.jpg"" />

<BitImage Width=""10rem""
          AspectRatio=""1""
          ImagePosition=""bottom""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph cropped to its bottom edge""
          Class=""framed""
          Src=""images/carousel/img2.jpg"" />";

    private readonly string example6RazorCode = @"
<BitImage Height=""96""
          Alt=""The bit platform logo""
          Class=""framed""
          ImageFit=""BitImageFit.CenterCover""
          Cover=""BitImageCover.Landscape""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""144""
          Width=""96""
          Alt=""The bit platform logo""
          Class=""framed""
          ImageFit=""BitImageFit.CenterCover""
          Cover=""BitImageCover.Portrait""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""The bit platform logo""
          Class=""framed""
          ImageFit=""BitImageFit.CenterContain""
          Cover=""BitImageCover.Landscape""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""144""
          Width=""96""
          Alt=""The bit platform logo""
          Class=""framed""
          ImageFit=""BitImageFit.CenterContain""
          Cover=""BitImageCover.Portrait""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example7RazorCode = @"
<div class=""max-frame-host"">
    <BitImage Alt=""A landscape photograph""
              MaximizeFrame
              Src=""images/carousel/img3.jpg"" />
</div>

<div class=""max-frame-host"">
    <BitImage Alt=""A landscape photograph""
              MaximizeFrame
              ImageFit=""BitImageFit.Contain""
              Src=""images/carousel/img3.jpg"" />
</div>";

    private readonly string example8RazorCode = @"
<BitImage Rounded
          Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph in a rounded frame""
          Src=""images/carousel/img4.jpg"" />

<BitImage Circular
          Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph in a circular frame""
          Src=""images/carousel/img4.jpg"" />

<BitImage Bordered
          Rounded
          Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph in a bordered, rounded frame""
          Src=""images/carousel/img4.jpg"" />

<BitImage Shadow
          Rounded
          Width=""10rem""
          AspectRatio=""1""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph in a raised, rounded frame""
          Src=""images/carousel/img4.jpg"" />";

    private readonly string example9RazorCode = @"
<BitButton OnClick=""() => loadLoading = true"">Load a slow image</BitButton>
<BitButton OnClick=""() => loadError = true"">Load a broken image</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""ReloadImages"">Reload both</BitButton>
<div>State: <b>@loadingStateText</b></div>

@if (loadLoading)
{
    <BitImage @ref=""slowImage""
              Width=""200px""
              Alt=""An image served with a delay""
              Src=""/api/Image/GetImage""
              OnLoadingStateChange=""s => loadingStateText = s.ToString()"">
        <LoadingTemplate>
            <BitSpinnerLoading CustomSize=""24"" />
            <span>loading...</span>
        </LoadingTemplate>
    </BitImage>
}

@if (loadError)
{
    <BitImage @ref=""brokenImage""
              Width=""200px""
              Alt=""An image whose source fails""
              Src=""/api/Image/GetImageError"">
        <LoadingTemplate><span>loading...</span></LoadingTemplate>
        <ErrorTemplate>
            <BitMessage Color=""BitColor.Error"">The image could not be loaded.</BitMessage>
        </ErrorTemplate>
    </BitImage>
}";
    private readonly string example9CsharpCode = @"
private bool loadLoading;
private bool loadError;
private string loadingStateText = ""Loading"";
private BitImage? slowImage;
private BitImage? brokenImage;

private async Task ReloadImages()
{
    if (slowImage is not null)
    {
        await slowImage.ReloadAsync();
    }

    if (brokenImage is not null)
    {
        await brokenImage.ReloadAsync();
    }
}";

    private readonly string example10RazorCode = @"
<BitImage Width=""9rem""
          Alt=""The bit platform logo, shown in place of a missing image""
          Src=""images/no-such-image.png""
          FallbackSrc=""images/bit-logo-blue.png"" />

<BitImage Width=""9rem""
          Alt=""The bit platform logo, shown in place of a missing image""
          FallbackSrc=""images/bit-logo-blue.png"" />";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""() => loadPlaceholder = !loadPlaceholder"">@(loadPlaceholder ? ""Reset"" : ""Load the image"")</BitButton>

@if (loadPlaceholder)
{
    <BitImage FadeIn
              Rounded
              Width=""16rem""
              AspectRatio=""16/9""
              ImageFit=""BitImageFit.Cover""
              Alt=""An image served with a delay""
              Src=""/api/Image/GetImage""
              PlaceholderSrc=""@placeholderDataUri"" />
}";

    private readonly string example12RazorCode = @"
<BitButton OnClick=""() => fadeKey++"">Load again</BitButton>

<BitImage @key=""@($""fade-{fadeKey}"")""
          FadeIn
          Width=""200px""
          Alt=""An image served with a delay""
          Src=""@($""/api/Image/GetImage?v={fadeKey}"")"" />

<BitImage @key=""@($""start-{fadeKey}"")""
          StartVisible
          Width=""200px""
          Alt=""An image served with a delay""
          Src=""@($""/api/Image/GetImage?v={fadeKey}"")"" />";

    private readonly string example13RazorCode = @"
<BitImage Alt=""A landscape photograph""
          Width=""12rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Loading=""BitImageLoading.Lazy""
          Decoding=""BitImageDecoding.Async""
          FetchPriority=""BitImageFetchPriority.Low""
          Src=""images/carousel/img1.jpg"" />

<BitImage Alt=""A landscape photograph""
          Width=""12rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          FetchPriority=""BitImageFetchPriority.High""
          ImageAttributes=""@(new() { { ""elementtiming"", ""hero"" } })""
          Src=""images/carousel/img2.jpg"" />";

    private readonly string example14RazorCode = @"
<BitImage Rounded
          Width=""100%""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph served at the size the viewport needs""
          Sizes=""(max-width: 600px) 100vw, 32rem""
          Srcset=""images/carousel/img1.jpg 1200w, images/carousel/img2.jpg 600w""
          Src=""images/carousel/img1.jpg"" />";

    private readonly string example15RazorCode = @"
<BitImage Rounded
          Width=""100%""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph, cropped differently on a narrow viewport""
          Src=""images/carousel/img1.jpg""
          Sources=""@(new BitImageSource[]
                     {
                         new() { Media = ""(max-width: 600px)"", Srcset = ""images/carousel/img4.jpg"" },
                         new() { Srcset = ""images/carousel/img1.jpg"" }
                     })"" />";

    private readonly string example16RazorCode = @"
<BitImage Rounded
          Width=""8rem""
          AspectRatio=""1""
          Draggable=""false""
          ImageFit=""BitImageFit.Cover""
          Alt=""Count this click""
          OnClick=""() => clickCount++""
          Src=""images/carousel/img3.jpg"" />

<BitImage Rounded
          Width=""8rem""
          AspectRatio=""1""
          IsEnabled=""false""
          Draggable=""false""
          ImageFit=""BitImageFit.Cover""
          Alt=""This one is disabled""
          OnClick=""() => clickCount++""
          Src=""images/carousel/img3.jpg"" />

<div>Clicked <b>@clickCount</b> times</div>";
    private readonly string example16CsharpCode = @"
private int clickCount;";

    private readonly string example17RazorCode = @"
<style>
    .image-caption {
        left: 0;
        right: 0;
        bottom: 0;
        color: white;
        padding: 0.75rem;
        position: absolute;
        background: linear-gradient(transparent, rgba(0, 0, 0, 0.65));
    }
</style>

<BitImage Rounded
          Width=""20rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""A landscape photograph""
          Src=""images/carousel/img2.jpg"">
    <div class=""image-caption"">A caption laid over the image</div>
</BitImage>";

    private readonly string example18RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        filter: hue-rotate(45deg);
        background-color: blueviolet;
    }

    .custom-image {
        width: 16rem;
        filter: opacity(25%);
        border-radius: 1rem 3rem;
    }
</style>

<BitImage Alt=""The bit platform logo""
          Style=""border: 2px solid goldenrod; border-radius: 5px; width: 258px;""
          Src=""images/bit-logo-blue.png"" />

<BitImage Alt=""The bit platform logo""
          Class=""custom-class""
          Src=""images/bit-logo-blue.png"" />


<BitImage Alt=""The bit platform logo""
          Styles=""@(new() { Image = ""filter: blur(5px)"" })""
          Src=""images/bit-logo-blue.png"" />

<BitImage Alt=""The bit platform logo""
          Classes=""@(new() { Image = ""custom-image"" })""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example19RazorCode = @"
<BitImage Rounded
          Dir=""BitDir.Rtl""
          Width=""20rem""
          AspectRatio=""16/9""
          ImageFit=""BitImageFit.Cover""
          Alt=""عکسی از یک منظره""
          Src=""images/carousel/img4.jpg"">
    <div class=""image-caption"">نوشته‌ای روی تصویر</div>
</BitImage>";
}
