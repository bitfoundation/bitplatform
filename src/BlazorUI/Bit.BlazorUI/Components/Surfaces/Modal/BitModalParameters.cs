namespace Bit.BlazorUI;

public class BitModalParameters
{
    public bool? IsEnabled { get; set; }

    public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    public BitDir? Dir { get; set; }

    public bool? AriaModal { get; set; }


    public bool? Blocking { get; set; }

    public BitModalClassStyles? Classes { get; set; }

    public bool? FullHeight { get; set; }

    public bool? FullWidth { get; set; }

    public bool? IsAlert { get; set; }

    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    public bool? ShowOverlay { get; set; }

    public BitModalClassStyles? Styles { get; set; }

    public string? SubtitleAriaId { get; set; }

    public string? TitleAriaId { get; set; }


    public static BitModalParameters? Merge(BitModalParameters? params1, BitModalParameters? params2)
    {
        if (params1 is null && params2 is null) return null;

        if (params2 is null) return params1;
        if (params1 is null) return params2;


        return new BitModalParameters
        {
            IsEnabled = params1.IsEnabled ?? params2.IsEnabled,
            HtmlAttributes = params2.HtmlAttributes.Concat(params1.HtmlAttributes).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value),
            Dir = params1.Dir ?? params2.Dir,
            AriaModal = params1.AriaModal ?? params2.AriaModal,
            Blocking = params1.Blocking ?? params2.Blocking,
            Classes = BitModalClassStyles.Merge(params1.Classes, params2.Classes),
            FullHeight = params1.FullHeight ?? params2.FullHeight,
            FullWidth = params1.FullWidth ?? params2.FullWidth,
            IsAlert = params1.IsAlert ?? params2.IsAlert,
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
            ShowOverlay = params1.ShowOverlay ?? params2.ShowOverlay,
            Styles = BitModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
        };
    }
}
