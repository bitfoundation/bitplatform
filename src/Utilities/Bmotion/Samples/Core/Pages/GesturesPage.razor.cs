namespace Bit.Bmotion.Samples.Core.Pages;

public partial class GesturesPage
{
    // The track is laid out in app.css to match: a 40px thumb travelling 4 × 44px.
    private const int Stops = 5;
    private const int StopSpacing = 44;
    private const int ThumbSize = 40;

    // Kept in fields rather than built in the markup: a drag is re-attached, which ends it, when its configuration
    // changes, and OnDrag re-renders this page on every move.
    private static readonly BmDragConstraints InParent = BmDragConstraints.Parent();
    private static readonly BmPreset _button = BmPresets.HoverLift() + BmPresets.Press();
    private static readonly BmInertia _snap = Bm.Inertia(modifyTarget: Bm.SnapTo(StopSpacing));

    private int _taps;
    private int _hovers;
    private int _drags;
    private int _moves;

    private string DragNote => RendererInfo.Name switch
    {
        "WebAssembly" => "in this process: OnDrag runs on every pointer move, as it happens",
        "Server" => "in the browser at once; .NET hears of each move a round trip later, and OnDrag runs at most once a frame",
        "WebView" => "in the WebView at once; .NET in the app hears of each move asynchronously, with no network in between",
        _ => "not yet: gestures start once a runtime takes over from the prerendered page",
    };
}
