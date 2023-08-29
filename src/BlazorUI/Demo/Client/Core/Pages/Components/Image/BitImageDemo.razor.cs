namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Image;

public partial class BitImageDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Alt",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies an alternate text for the image."
        },
        new()
        {
            Name = "AriaDescription",
            Type = "string?",
            DefaultValue = "null",
            Description = "Detailed description of the image for the benefit of screen readers."
        },
        new()
        {
            Name = "AriaHidden",
            Type = "bool",
            DefaultValue = "false",
            Description = "If true, add an aria-hidden attribute instructing screen readers to ignore the element."
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
            Name = "CoverStyle",
            Type = "BitImageCoverStyle",
            DefaultValue = "null",
            Description = "Specifies the cover style to be used for this image.",
            LinkType = LinkType.Link,
            Href = "#image-cover-enum"
        },
        new()
        {
            Name = "ErrorSrc",
            Type = "string?",
            DefaultValue = "null",
            Description = "Specifies the error src of the image."
        },
        new()
        {
            Name = "Height",
            Type = "double?",
            DefaultValue = "null",
            Description = "The image height value in px."
        },
        new()
        {
            Name = "ImageFit",
            Type = "BitImageFit?",
            DefaultValue = "null",
            Description = "Used to determine how the image is scaled and cropped to fit the frame.",
            LinkType = LinkType.Link,
            Href = "#image-fit-enum"
        },
        new()
        {
            Name = "Loading",
            Type = "string?",
            DefaultValue = "null",
            Description = "Allows for browser-level image loading (lazy or eager)."
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
            Type = "EventCallback<BitImageLoadingState>",
            DefaultValue = "null",
            Description = "Optional callback method for when the image load state has changed.",
            LinkType = LinkType.Link,
            Href = "#image-loading-enum"
        },
        new()
        {
            Name = "ShouldFadeIn",
            Type = "bool",
            DefaultValue = "true",
            Description = "If true, fades the image in when loaded."
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
            Name = "ShouldStartVisible",
            Type = "bool",
            DefaultValue = "true",
            Description = "If true, the image starts as visible and is hidden on error. Otherwise, the image is hidden until it is successfully loaded."
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
            Type = "double?",
            DefaultValue = "null",
            Description = "The image width value in px."
        }
    };

    private readonly List<ComponentSubClass> componentSubClasses = new()
    {
        new()
        {
            Id = "image-class-style",
            Title = "BitImageClassStyles",
            Description = "",
            Parameters = new()
            {
                new()
                {
                    Name= "Image",
                    Type = "string?",
                    DefaultValue = "null",
                    Description="Custom CSS classes/styles for the image element."
                },
                new()
                {
                    Name= "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description="Custom CSS classes/styles for the image container."
                }
            }
        }
    };

    private readonly List<ComponentSubEnum> componentSubEnums = new()
    {
        new()
        {
            Id = "image-fit-enum",
            Name = "BitImageFit",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "Button",
                    Description="The button is a clickable button.",
                    Value="0",
                },
                new()
                {
                    Name= "Submit",
                    Description="The button is a submit button (submits form-data).",
                    Value="1",
                },
                new()
                {
                    Name= "Reset",
                    Description="The button is a reset button (resets the form-data to its initial values).",
                    Value="2",
                }
            }
        },
        new()
        {
            Id = "image-cover-enum",
            Name = "BitImageCoverStyle",
            Description = "",
            Items = new()
            {
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
            }
        },
        new()
        {
            Id = "image-loading-enum",
            Name = "BitImageLoadingState",
            Description = "",
            Items = new()
            {
                new()
                {
                    Name= "NotLoaded",
                    Description="The image has not yet been loaded, and there is no error yet.",
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
                },
                new()
                {
                    Name= "ErrorLoaded",
                    Description="Not used. Use `OnLoadingStateChange` and re-render the Image with a different src.",
                    Value="3",
                }
            }
        }
    };



    private readonly string example1HtmlCode = @"
<BitImage Alt=""Basic BitImage"" Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />";

    private readonly string example2HtmlCode = @"
<BitLabel>Width</BitLabel>
<BitImage Width=""144""
          Alt=""BitImage with width""
          Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
                
<BitLabel>Height</BitLabel>
<BitImage Height=""80""
          Alt=""BitImage with height""
          Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
             
<BitLabel>Width & Height</BitLabel>
<BitImage Width=""256""
          Height=""128""
          Alt=""BitImage with width and height""
          Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />";

    private readonly string example3HtmlCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        filter: hue-rotate(45deg);
        background-color: blueviolet;
    }

    .custom-image {
        filter: opacity(25%);
    }

    .custom-container {
        width: rem2(256px);
        border-radius: rem2(16px) rem2(48px);
    }
<style>

<BitTypography Variant=""BitTypographyVariant.H6"">For container only</BitTypography> 
<BitLabel>Styled</BitLabel>
<BitImage Alt=""Styled BitImage""
            Style=""border: 2px solid goldenrod; border-radius: 5px; width: 258px;""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
                    
<BitLabel>Classed</BitLabel>
<BitImage Alt=""Styled BitImage""
            Class=""custom-class""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
                   

<BitTypography Variant=""BitTypographyVariant.H6"">For each part</BitTypography>            
<BitLabel>Styles</BitLabel>
<BitImage Alt=""Styled BitImage""
            Styles=""@(new() { Container = ""border: 1px solid tomato"", Image = ""filter: blur(5px)"" })""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />
                    
<BitLabel>Classes</BitLabel>
<BitImage Alt=""Styled BitImage""
            Classes=""@(new() { Container = ""custom-container"", Image = ""custom-image"" })""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />";

    private readonly string example4HtmlCode = @"
Visible: [
<BitImage Alt=""Visible BitImage""
            Width=""194""
            Visibility=""BitVisibility.Visible""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" /> ]

Hidden: [
<BitImage Alt=""Hidden BitImage""
            Width=""194""
            Visibility=""BitVisibility.Hidden""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" /> ]

Collapsed: [
<BitImage Alt=""Collapsed BitImage""
            Width=""194""
            Visibility=""BitVisibility.Collapsed""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" /> ]";

    private readonly string example5HtmlCode = @"
<BitLabel>None</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.None""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>Center</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.Center""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>Contain</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.Contain""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>Cover</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.Cover""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>CenterContain</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.CenterContain""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>CenterCover</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            ImageFit=""BitImageFit.CenterCover""
            Style=""background-color: #00ffff17""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />";

    private readonly string example6HtmlCode = @"
<BitTypography Variant=""BitTypographyVariant.H6"">ImageFit: CenterCover</BitTypography>
<BitLabel>Landscape</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            Style=""background-color: #00ffff17""
            ImageFit=""BitImageFit.CenterCover""
            CoverStyle=""BitImageCoverStyle.Landscape""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>Portrait</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            Style=""background-color: #00ffff17""
            ImageFit=""BitImageFit.CenterCover""
            CoverStyle=""BitImageCoverStyle.Portrait""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />


<BitTypography Variant=""BitTypographyVariant.H6"">ImageFit: CenterContain</BitTypography>
<BitLabel>Landscape</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            Style=""background-color: #00ffff17""
            ImageFit=""BitImageFit.CenterContain""
            CoverStyle=""BitImageCoverStyle.Landscape""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />

<BitLabel>Portrait</BitLabel>
<BitImage Height=""96""
            Alt=""Basic BitImage""
            Style=""background-color: #00ffff17""
            ImageFit=""BitImageFit.CenterContain""
            CoverStyle=""BitImageCoverStyle.Portrait""
            Src=""_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" />";
}
