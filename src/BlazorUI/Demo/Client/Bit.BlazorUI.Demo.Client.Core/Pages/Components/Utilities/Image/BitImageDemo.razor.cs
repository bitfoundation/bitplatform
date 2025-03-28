﻿namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Image;

public partial class BitImageDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alt",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies an alternate text for the image."
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
            Description = "Specifies the cover style to be used for this image.",
            LinkType = LinkType.Link,
            Href = "#image-cover-style"
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
            DefaultValue = "true",
            Description = "If true, fades the image in when loaded."
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The image height value."
        },
        new()
        {
            Name = "ImageAttributes",
            Type = "Dictionary<string, object>",
            DefaultValue = "new Dictionary<string, object>()",
            Description = "Capture and render additional attributes in addition to the image's parameters"
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
            Description = "Callback for when the image is clicked."
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
            Name = "StartVisible",
            Type = "bool",
            DefaultValue = "true",
            Description = "If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded."
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the src of the image."
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
            Description = "The image width value."
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
                    Name= "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description="Custom CSS classes/styles for the image element."
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
                    Description="Neither the image nor the frame are scaled.",
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
                    Description="The image will be centered horizontally and vertically within the frame and maintains its aspect ratio.",
                    Value="2",
                },
                new()
                {
                    Name= "CenterCover",
                    Description="The image will be centered horizontally and vertically within the frame and maintains its aspect ratio.",
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
                    Value="4",
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
                    Description="An error has been encountered while loading the image.",
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
    ];


    private bool loadLoading;
    private bool loadError;



    private readonly string example1RazorCode = @"
<BitImage Alt=""Basic BitImage"" Src=""images/bit-logo-blue.png"" />

<BitImage Alt=""Disabled BitImage"" IsEnabled=""false"" Src=""images/bit-logo-blue.png"" />";

    private readonly string example2RazorCode = @"
<BitImage Width=""9rem""
          Alt=""BitImage with width""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />
                
<BitImage Height=""5rem""
          Alt=""BitImage with height""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />
             
<BitImage Width=""256px""
          Height=""128px""
          Alt=""BitImage with width and height""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example3RazorCode = @"
<BitImage Height=""96""
          Alt=""ImageFit: None BitImage""
          ImageFit=""BitImageFit.None""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""ImageFit: Center BitImage""
          ImageFit=""BitImageFit.Center""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""ImageFit: Contain BitImage""
          ImageFit=""BitImageFit.Contain""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""ImageFit: Cover BitImage""
          ImageFit=""BitImageFit.Cover""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""ImageFit: CenterContain BitImage""
          ImageFit=""BitImageFit.CenterContain""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""96""
          Alt=""ImageFit: CenterCover BitImage""
          ImageFit=""BitImageFit.CenterCover""
          Style=""background-color: #00ffff17""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example4RazorCode = @"
<BitImage Height=""96""
          Alt=""Landscape BitImage with ImageFit: CenterCover""
          Style=""background-color: #00ffff17""
          ImageFit=""BitImageFit.CenterCover""
          Cover=""BitImageCover.Landscape""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""144""
          Width=""96""
          Alt=""Portrait BitImage with ImageFit: CenterCover""
          Style=""background-color: #00ffff17""
          ImageFit=""BitImageFit.CenterCover""
          Cover=""BitImageCover.Portrait""
          Src=""images/bit-logo-blue.png"" />


<BitImage Height=""96""
          Alt=""Landscape BitImage with ImageFit: CenterContain""
          Style=""background-color: #00ffff17""
          ImageFit=""BitImageFit.CenterContain""
          Cover=""BitImageCover.Landscape""
          Src=""images/bit-logo-blue.png"" />

<BitImage Height=""144""
          Width=""96""
          Alt=""Portrait BitImage with ImageFit: CenterContain""
          Style=""background-color: #00ffff17""
          ImageFit=""BitImageFit.CenterContain""
          Cover=""BitImageCover.Portrait""
          Src=""images/bit-logo-blue.png"" />";

    private readonly string example5RazorCode = @"
<BitImage Alt=""Maximized BitImage"" MaximizeFrame=""true"" Src=""images/bit-logo-blue.png"" />";

    private readonly string example6RazorCode = @"
<BitButton OnClick=""() => loadLoading = true"">Load image</BitButton>
@if (loadLoading)
{
    <BitImage Alt=""Loading ImageState"" Src=""/api/Image/GetImage"" Width=""200px"">
        <LoadingTemplate>
            <b>loading...</b>
        </LoadingTemplate>
    </BitImage>
}

<BitButton OnClick=""() => loadError = true"">Load image</BitButton>
@if (loadError)
{
    <BitImage Alt=""Error ImageState"" Src=""/api/Image/GetImageError"" Width=""200px"">
        <LoadingTemplate><b>...</b></LoadingTemplate>
        <ErrorTemplate>
            <b>error!!!</b>
        </ErrorTemplate>
    </BitImage>
}";
    private readonly string example6CsharpCode = @"
private bool loadLoading;
private bool loadError;
";

    private readonly string example7RazorCode = @"
< style>
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

<BitImage Alt=""Styled BitImage""
          Style=""border: 2px solid goldenrod; border-radius: 5px; width: 258px;""
          Src=""images/bit-logo-blue.png"" />
                    
<BitImage Alt=""Styled BitImage""
          Class=""custom-class""
          Src=""images/bit-logo-blue.png"" />
                   

<BitImage Alt=""Styled BitImage""
          Styles=""@(new() { Image = ""filter: blur(5px)"" })""
          Src=""images/bit-logo-blue.png"" />
                    
<BitImage Alt=""Styled BitImage""
          Classes=""@(new() { Image = ""custom-image"" })""
          Src=""images/bit-logo-blue.png"" />";
}
