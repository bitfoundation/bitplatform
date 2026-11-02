using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Maui.Controls.Shapes;

namespace Bit.BbScanner;

/// <summary>
/// Hosts a <see cref="BlazorWebView"/> whose scanners use the device's own camera: wrap the BlazorWebView in one, and the
/// camera's native preview is drawn behind it, where the scanner's video element is, under the scanner's overlays and
/// controls.
/// </summary>
/// <remarks>
/// <para>
/// A native preview cannot be drawn inside the page, so it is drawn behind the WebView, and the page lets it show through:
/// while a camera is open the scanner, and every element above it in the page, paints no background, the WebView is made
/// transparent, and the host paints the page's own background colour behind it, so the page around the scanner looks as
/// it did. An element above the scanner that paints a colour of its own - a card it sits in, say - shows the page's
/// colour while the camera is open.
/// </para>
/// <para>
/// The page reports where the video element is on every animation frame it moves on, scrolling included, and the preview
/// follows; it lags the page by a frame or so while it scrolls. It is cut to whatever clips the scanner in the page - a
/// scrolling panel it is partly scrolled out of, say.
/// </para>
/// <para>
/// With more than one host, a camera draws in the one loaded last, so the scanner belongs in that host's BlazorWebView.
/// Only the scanner's own ancestors are made transparent: anything else the page paints under the scanner - the content
/// and backdrop beneath a dialog the scanner is in, say - covers the preview.
/// </para>
/// </remarks>
/// <example>
/// <code language="xml">
/// &lt;bb:BbScannerHost&gt;
///     &lt;BlazorWebView HostPage="wwwroot/index.html"&gt; ... &lt;/BlazorWebView&gt;
/// &lt;/bb:BbScannerHost&gt;
/// </code>
/// </example>
[ContentProperty(nameof(Content))]
public partial class BbScannerHost : Grid
{
    /// <summary>The hosted view: the BlazorWebView, or a layout that holds it.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(
        nameof(Content), typeof(View), typeof(BbScannerHost), propertyChanged: (bindable, previous, next) =>
            ((BbScannerHost)bindable).OnContentChanged(previous as View, next as View));

    /// <summary>
    /// How long a preview stays up, unclaimed, after its camera closed: long enough for the next camera of a switch to
    /// claim it, so the page does not flash its backgrounds back in between.
    /// </summary>
    private static readonly TimeSpan HandOver = TimeSpan.FromSeconds(1.5);

    private static readonly List<WeakReference<BbScannerHost>> Hosts = [];

    private readonly AbsoluteLayout _layer;
    private readonly Border _frame;
    private BbPreviewClaim? _owner;
    private int _releases;
    private BbPreviewPlacement _placement;
    private Color? _background;
    private bool _transparent;

    /// <summary>Creates a host; set <see cref="Content"/> to the BlazorWebView.</summary>
    public BbScannerHost()
    {
        Preview = new BbCameraPreview();

        // Black until the camera's first frame, as the page's video element would be.
        _frame = new Border
        {
            StrokeThickness = 0,
            Stroke = Colors.Transparent,
            BackgroundColor = Colors.Black,
            Padding = 0,
            Content = Preview,
            InputTransparent = true,
        };

        _layer = new AbsoluteLayout
        {
            IsVisible = false,
            InputTransparent = true,
            CascadeInputTransparent = true,
            IsClippedToBounds = true,
        };
        _layer.Children.Add(_frame);
        Children.Add(_layer);

        Loaded += (_, _) => Register(this);
        Unloaded += (_, _) => Unregister(this);
    }

    /// <summary>The hosted view: the BlazorWebView, or a layout that holds it.</summary>
    public View? Content
    {
        get => (View?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>The view a camera session draws its preview into.</summary>
    internal BbCameraPreview Preview { get; }

    /// <summary>The host a camera opened now draws its preview in: the last one loaded that still is.</summary>
    internal static BbScannerHost? Active
    {
        get
        {
            lock (Hosts)
            {
                Hosts.RemoveAll(host => !host.TryGetTarget(out _));
                for (var i = Hosts.Count - 1; i >= 0; i--)
                {
                    if (Hosts[i].TryGetTarget(out var host) && host.IsLoaded && host.Preview.Handler is not null)
                    {
                        return host;
                    }
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Gives the preview to a camera about to open, on the UI thread: placements from an earlier camera are ignored from
    /// now on.
    /// </summary>
    /// <remarks>
    /// The preview view is laid out from here on, hidden behind the WebView until the page says where it goes - a camera
    /// that draws into a view that is not laid out delivers no frames on some platforms, Android's among them.
    /// </remarks>
    internal BbPreviewClaim Claim()
    {
        var claim = new BbPreviewClaim(this);
        _owner = claim;
        if (!_transparent)
        {
            Park();
        }

        return claim;
    }

    /// <summary>Puts the preview where the page says the scanner's video element is, or takes it down.</summary>
    internal void Place(BbPreviewClaim claim, BbPreviewPlacement placement) => Dispatcher.Dispatch(() =>
    {
        if (claim != _owner)
        {
            return;
        }

        if (placement.IsDetached)
        {
            TakeDown();
        }
        else
        {
            Apply(placement);
        }
    });

    /// <summary>
    /// A camera closed. The preview stays up a moment, for the next camera of a switch to claim, and comes down unless
    /// one does.
    /// </summary>
    internal void Release(BbPreviewClaim claim) => Dispatcher.Dispatch(() =>
    {
        if (claim != _owner)
        {
            return;
        }

        _owner = null;
        if (!_transparent)
        {
            TakeDown();
            return;
        }

        var release = ++_releases;
        Dispatcher.DispatchDelayed(HandOver, () =>
        {
            if (_owner is null && release == _releases)
            {
                TakeDown();
            }
        });
    });

    private void Apply(BbPreviewPlacement placement)
    {
        var webView = FindWebView();
        if (webView is null)
        {
            return;
        }

        if (!placement.Visible || placement.Width <= 0 || placement.Height <= 0)
        {
            Park();
            return;
        }

        if (!_transparent)
        {
            _transparent = true;
            _background = BackgroundColor;
            SetWebViewTransparent(webView, true);
        }

        // A page with no colour of its own shows the WebView's canvas, which is white.
        BackgroundColor = placement.Backdrop is { } backdrop && Color.TryParse(backdrop, out var colour) ? colour : Colors.White;

        // The page's CSS pixels are the WebView's device independent units. The WebView may sit anywhere in the host,
        // and the layer too - each inset by the safe area, say - so the preview goes where the WebView is from the
        // layer's point of view.
        var (left, top) = WebViewOrigin(webView);
        AbsoluteLayout.SetLayoutBounds(_frame, new Rect(left + placement.X, top + placement.Y, placement.Width, placement.Height));
        if (Math.Abs(placement.CornerRadius - _placement.CornerRadius) > 0.01 || _frame.StrokeShape is null)
        {
            _frame.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(placement.CornerRadius) };
        }

        _layer.Clip = new RectangleGeometry(new Rect(left + placement.ClipX, top + placement.ClipY, placement.ClipWidth, placement.ClipHeight));
        _frame.Opacity = 1;
        _placement = placement;
        _layer.IsVisible = true;
    }

    /// <summary>Keeps the preview laid out, a point in size and transparent, where the WebView covers it.</summary>
    private void Park()
    {
        var (left, top) = FindWebView() is { } webView ? WebViewOrigin(webView) : (0, 0);
        AbsoluteLayout.SetLayoutBounds(_frame, new Rect(left, top, 1, 1));
        _frame.Opacity = 0;
        _layer.Clip = null;
        _layer.IsVisible = true;
    }

    /// <summary>Takes the preview down and puts the WebView back as it was.</summary>
    private void TakeDown()
    {
        _placement = default;
        _layer.IsVisible = false;
        _layer.Clip = null;
        if (_transparent)
        {
            _transparent = false;
            BackgroundColor = _background;
            if (FindWebView() is { } webView)
            {
                SetWebViewTransparent(webView, false);
            }
        }
    }

    private BlazorWebView? FindWebView()
    {
        if (Content is BlazorWebView webView)
        {
            return webView;
        }

        return Content is IVisualTreeElement content
            ? content.GetVisualTreeDescendants().OfType<BlazorWebView>().FirstOrDefault()
            : null;
    }

    /// <summary>Where the WebView's top left corner is in the preview layer.</summary>
    private (double Left, double Top) WebViewOrigin(BlazorWebView webView)
    {
        var (webViewLeft, webViewTop) = OffsetOf(webView);
        var (layerLeft, layerTop) = OffsetOf(_layer);
        return (webViewLeft - layerLeft, webViewTop - layerTop);
    }

    private (double Left, double Top) OffsetOf(View view)
    {
        double left = 0, top = 0;
        for (Element? element = view; element is VisualElement visual && element != this; element = element.Parent)
        {
            left += visual.Frame.X;
            top += visual.Frame.Y;
        }

        return (left, top);
    }

    private void OnContentChanged(View? previous, View? next)
    {
        if (previous is not null)
        {
            Children.Remove(previous);
        }

        // Above the preview layer: the page is drawn over the camera.
        if (next is not null)
        {
            Children.Add(next);
        }
    }

    /// <summary>Makes the platform's WebView let what is behind it show through, or puts it back.</summary>
    partial void SetWebViewTransparent(BlazorWebView webView, bool transparent);

    private static void Register(BbScannerHost host)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(entry => !entry.TryGetTarget(out var target) || target == host);
            Hosts.Add(new WeakReference<BbScannerHost>(host));
        }
    }

    private static void Unregister(BbScannerHost host)
    {
        lock (Hosts)
        {
            Hosts.RemoveAll(entry => !entry.TryGetTarget(out var target) || target == host);
        }
    }
}

/// <summary>
/// A camera's hold on a <see cref="BbScannerHost"/>'s preview: the host draws the placements of the latest claim only,
/// so a camera closing late never takes down the preview of the one that replaced it.
/// </summary>
internal sealed class BbPreviewClaim(BbScannerHost host)
{
    public BbScannerHost Host { get; } = host;

    /// <summary>The view the camera draws its preview into.</summary>
    public BbCameraPreview Preview => Host.Preview;

    public void Place(BbPreviewPlacement placement) => Host.Place(this, placement);

    public void Release() => Host.Release(this);
}
