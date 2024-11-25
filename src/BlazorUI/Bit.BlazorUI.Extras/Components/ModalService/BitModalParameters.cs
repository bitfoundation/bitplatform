namespace Bit.BlazorUI;

public class BitModalParameters
{
    public bool AutoToggleScroll { get; set; }

    public bool AbsolutePosition { get; set; }

    public bool Blocking { get; set; }

    public BitModalClassStyles? Classes { get; set; }

    public string? DragElementSelector { get; set; }

    public bool Draggable { get; set; }

    public bool FullHeight { get; set; }

    public bool FullSize { get; set; }

    public bool FullWidth { get; set; }

    public bool? IsAlert { get; set; }

    public bool IsOpen { get; set; }

    public bool Modeless { get; set; }

    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    public BitModalPosition? Position { get; set; }

    public string? ScrollerSelector { get; set; }

    public BitModalClassStyles? Styles { get; set; }

    public string? SubtitleAriaId { get; set; }

    public string? TitleAriaId { get; set; }
}
