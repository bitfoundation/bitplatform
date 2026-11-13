namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ImageViewer;

public partial class BitImageViewerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Alt",
            Type = "string?",
            DefaultValue = "null",
            Description = "The alternative text of the image, which also names the viewer where it has no AriaLabel.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitImageViewerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for the different parts of the image viewer.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "DoubleClickZoom",
            Type = "double",
            DefaultValue = "2",
            Description = "The zoom a double-click (or a double tap) goes to, at the point it lands on, and back to fitted from any zoom above it.",
        },
        new()
        {
            Name = "Height",
            Type = "string?",
            DefaultValue = "null",
            Description = "The height of the frame the image is shown in, as a CSS value (e.g. \"24rem\"). Default: the --bit-ImageViewer-height variable, else 20rem.",
        },
        new()
        {
            Name = "MaxZoom",
            Type = "double",
            DefaultValue = "8",
            Description = "The largest zoom, as a multiple of the fitted image.",
        },
        new()
        {
            Name = "MinZoom",
            Type = "double",
            DefaultValue = "1",
            Description = "The smallest zoom, as a multiple of the fitted image: 1 is the fitted image itself.",
        },
        new()
        {
            Name = "NoToolbar",
            Type = "bool",
            DefaultValue = "false",
            Description = "Leaves the zoom buttons out, for a viewer operated by the pointer and the keyboard alone.",
        },
        new()
        {
            Name = "Src",
            Type = "string?",
            DefaultValue = "null",
            Description = "The URL of the image. A new one starts fitted again.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitImageViewerClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for the different parts of the image viewer.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Texts",
            Type = "BitImageViewerTexts?",
            DefaultValue = "null",
            Description = "The words of the viewer: the names of its buttons and the instructions of its keyboard.",
            LinkType = LinkType.Link,
            Href = "#texts",
        },
        new()
        {
            Name = "WheelZoom",
            Type = "bool",
            DefaultValue = "false",
            Description = "Zooms with the plain wheel too, not only with Ctrl (or Cmd) held. Off by default, since the wheel is what scrolls the page the viewer sits in.",
        },
        new()
        {
            Name = "Zoom",
            Type = "double",
            DefaultValue = "1",
            Description = "The zoom, as a multiple of the image fitted in the frame. Two-way bindable.",
        },
        new()
        {
            Name = "ZoomFactor",
            Type = "double",
            DefaultValue = "1.25",
            Description = "The factor a step of the buttons and the + and - keys multiplies or divides the zoom by.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "texts",
            Title = "BitImageViewerTexts",
            Parameters =
            [
                new() { Name = "RoleDescription", Type = "string", DefaultValue = "\"image viewer\"", Description = "What the viewer is announced as, after its name." },
                new() { Name = "Instructions", Type = "string", DefaultValue = "\"Plus and minus zoom, arrow keys move a zoomed image, 0 fits it again.\"", Description = "How the viewer is operated from the keyboard, which a screen reader reads once the viewer has the focus." },
                new() { Name = "ToolbarAriaLabel", Type = "string", DefaultValue = "\"Zoom\"", Description = "The name of the group of the zoom buttons." },
                new() { Name = "ZoomIn", Type = "string", DefaultValue = "\"Zoom in\"", Description = "The name of the zoom in button." },
                new() { Name = "ZoomOut", Type = "string", DefaultValue = "\"Zoom out\"", Description = "The name of the zoom out button." },
                new() { Name = "Reset", Type = "string", DefaultValue = "\"Fit to view\"", Description = "The name of the button that fits the image in the viewer again." },
            ]
        },
        new()
        {
            Id = "class-styles",
            Title = "BitImageViewerClassStyles",
            Parameters =
            [
                new() { Name = "Root", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the root element of the BitImageViewer." },
                new() { Name = "Viewport", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the viewport of the BitImageViewer, the frame the image is zoomed and panned in." },
                new() { Name = "Image", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the image of the BitImageViewer." },
                new() { Name = "Toolbar", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the toolbar of the BitImageViewer." },
                new() { Name = "Button", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for each button of the toolbar of the BitImageViewer." },
                new() { Name = "ZoomText", Type = "string?", DefaultValue = "null", Description = "Custom CSS classes/styles for the zoom level shown in the toolbar of the BitImageViewer." },
            ]
        },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new()
        {
            Name = "ZoomIn",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Zooms in by one step (ZoomFactor), around the center of the frame.",
        },
        new()
        {
            Name = "ZoomOut",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Zooms out by one step (ZoomFactor), around the center of the frame.",
        },
        new()
        {
            Name = "Reset",
            Type = "() => Task",
            DefaultValue = "",
            Description = "Fits the image in the frame again: zoom 1, centered.",
        },
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-ImageViewer-height", DefaultValue = "20rem", Description = "Height of the frame, where no Height is given." },
        new() { Name = "--bit-ImageViewer-background", DefaultValue = "var(--bit-clr-bg-ter)", Description = "Fill of the frame around the image." },
        new() { Name = "--bit-ImageViewer-radius", DefaultValue = "var(--bit-shp-radius-surface)", Description = "Corner radius of the frame." },
        new() { Name = "--bit-ImageViewer-button-size", DefaultValue = "spacing(4)", Description = "Width and height of a toolbar button." },
        new() { Name = "--bit-ImageViewer-button-hover-background", DefaultValue = "var(--bit-clr-bg-sec-hover)", Description = "Fill of a toolbar button under the pointer." },
        new() { Name = "--bit-ImageViewer-focus-color", DefaultValue = "var(--bit-clr-pri-focus)", Description = "Focus ring of the frame and the buttons." },
    ];



    private double boundZoom = 1;
    private BitImageViewer viewerRef = default!;

    private static readonly BitImageViewerTexts spanishTexts = new()
    {
        RoleDescription = "visor de imágenes",
        Instructions = "Más y menos amplían, las flechas mueven la imagen ampliada y 0 la ajusta de nuevo.",
        ToolbarAriaLabel = "Zoom",
        ZoomIn = "Acercar",
        ZoomOut = "Alejar",
        Reset = "Ajustar a la vista",
    };

    private static readonly BitImageViewerTexts persianTexts = new()
    {
        RoleDescription = "نمایشگر تصویر",
        Instructions = "مثبت و منفی بزرگ‌نمایی می‌کنند، کلیدهای جهت تصویر بزرگ‌شده را جابه‌جا می‌کنند و ۰ آن را دوباره جا می‌دهد.",
        ToolbarAriaLabel = "بزرگ‌نمایی",
        ZoomIn = "بزرگ‌نمایی",
        ZoomOut = "کوچک‌نمایی",
        Reset = "جا دادن در قاب",
    };
}
