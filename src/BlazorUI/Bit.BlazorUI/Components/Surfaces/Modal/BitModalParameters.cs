namespace Bit.BlazorUI;

public class BitModalParameters
{
    public bool IsEnabled { get { return _modal?.IsEnabled ?? field; } set; } = true;

    public Dictionary<string, object> HtmlAttributes { get { return _modal?.HtmlAttributes ?? field; } set; } = [];

    public BitDir? Dir { get { return _modal?.Dir ?? field; } set; }


    public bool AutoToggleScroll { get { return _modal?.AutoToggleScroll ?? field; } set; }

    public bool AbsolutePosition { get { return _modal?.AbsolutePosition ?? field; } set; }

    public bool Blocking { get { return _modal?.Blocking ?? field; } set; }

    public BitModalClassStyles? Classes { get; set; }

    public string? DragElementSelector { get { return _modal?.DragElementSelector ?? field; } set; }

    public bool Draggable { get { return _modal?.Draggable ?? field; } set; }

    public bool FullHeight { get { return _modal?.FullHeight ?? field; } set; }

    public bool FullSize { get { return _modal?.FullSize ?? field; } set; }

    public bool FullWidth { get { return _modal?.FullWidth ?? field; } set; }

    public bool? IsAlert { get { return _modal?.IsAlert ?? field; } set; }

    public bool Modeless { get { return _modal?.Modeless ?? field; } set; }

    public EventCallback<MouseEventArgs> OnDismiss
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                if (_modal is not null) await _modal.OnDismiss.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    public EventCallback<MouseEventArgs> OnOverlayClick
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                if (_modal is not null) await _modal.OnOverlayClick.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    public BitPosition? Position { get { return _modal?.Position ?? field; } set; }

    public string? ScrollerSelector { get { return _modal?.ScrollerSelector ?? field; } set; }

    public BitModalClassStyles? Styles { get; set; }

    public string? SubtitleAriaId { get { return _modal?.SubtitleAriaId ?? field; } set; }

    public string? TitleAriaId { get { return _modal?.TitleAriaId ?? field; } set; }


    private BitModal? _modal;
    public void SetModal(BitModal modal)
    {
        _modal = modal;
    }


    public static BitModalParameters? Merge(BitModalParameters? params1, BitModalParameters? params2)
    {
        if (params1 is null && params2 is null) return null;

        if (params2 is null) return params1;
        if (params1 is null) return params2;


        return new BitModalParameters
        {
            IsEnabled = (params1.IsEnabled is false || params2.IsEnabled is false) is false,
            HtmlAttributes = params1.HtmlAttributes.Concat(params2.HtmlAttributes).ToDictionary(kv => kv.Key, kv => kv.Value),
            Dir = params1.Dir ?? params2.Dir,
            AutoToggleScroll = params1.AutoToggleScroll || params2.AutoToggleScroll,
            AbsolutePosition = params1.AbsolutePosition || params2.AbsolutePosition,
            Blocking = params1.Blocking || params2.Blocking,
            Classes = BitModalClassStyles.Merge(params1.Classes, params2.Classes),
            DragElementSelector = params1.DragElementSelector ?? params2.DragElementSelector,
            Draggable = params1.Draggable || params2.Draggable,
            FullHeight = params1.FullHeight || params2.FullHeight,
            FullSize = params1.FullSize || params2.FullSize,
            FullWidth = params1.FullWidth || params2.FullWidth,
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            Modeless = params1.Modeless || params2.Modeless,
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                await params1.OnDismiss.InvokeAsync();
                await params2.OnDismiss.InvokeAsync();
            }),
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                await params1.OnOverlayClick.InvokeAsync();
                await params2.OnOverlayClick.InvokeAsync();
            }),
            Position = params1.Position ?? params2.Position,
            ScrollerSelector = params1.ScrollerSelector ?? params2.ScrollerSelector,
            Styles = BitModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
        };
    }
}
