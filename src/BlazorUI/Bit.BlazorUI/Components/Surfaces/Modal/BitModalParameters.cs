namespace Bit.BlazorUI;

public class BitModalParameters
{
    public bool IsEnabled { get { return _modal.IsEnabled || field; } set; } = true;
    
    public Dictionary<string, object> HtmlAttributes { get { return _modal.HtmlAttributes ?? field; } set; } = [];

    public BitDir? Dir { get { return _modal.Dir ?? field; } set; }


    public bool AutoToggleScroll { get { return _modal.AutoToggleScroll || field; } set; }

    public bool AbsolutePosition { get { return _modal.AbsolutePosition || field; } set; }

    public bool Blocking { get { return _modal.Blocking || field; } set; }

    public BitModalClassStyles? Classes { get; set; }

    public string? DragElementSelector { get { return _modal.DragElementSelector ?? field; } set; }

    public bool Draggable { get { return _modal.Draggable || field; } set; }

    public bool FullHeight { get { return _modal.FullHeight || field; } set; }

    public bool FullSize { get { return _modal.FullSize || field; } set; }

    public bool FullWidth { get { return _modal.FullWidth || field; } set; }

    public bool? IsAlert { get { return _modal.IsAlert ?? field; } set; }

    public bool Modeless { get { return _modal.Modeless || field; } set; }

    public EventCallback<MouseEventArgs> OnDismiss
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(_modal, async () =>
            {
                await OnDismiss.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    public EventCallback<MouseEventArgs> OnOverlayClick
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(_modal, async () =>
            {
                await OnOverlayClick.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    public BitModalPosition? Position { get { return _modal.Position ?? field; } set; }

    public string? ScrollerSelector { get { return _modal.ScrollerSelector ?? field; } set; }

    public BitModalClassStyles? Styles { get; set; }

    public string? SubtitleAriaId { get { return _modal.SubtitleAriaId ?? field; } set; }

    public string? TitleAriaId { get { return _modal.TitleAriaId ?? field; } set; }


    private BitModal _modal = default!;
    public void SetModal(BitModal modal)
    {
        _modal = modal;
    }
}
