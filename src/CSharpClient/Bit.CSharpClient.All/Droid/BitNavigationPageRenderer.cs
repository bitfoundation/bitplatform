#if Android
using Android.Content;
using Android.Support.V7.Widget;
using Bit.Droid;
using Bit.View;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(BitNavigationPage), typeof(BitNavigationPageRenderer))]

namespace Bit.Droid
{
    public class BitNavigationPageRenderer : NavigationPageRenderer
    {
        public BitNavigationPageRenderer(Context context)
            : base(context)
        {
        }

        private IVisualElementRenderer _navBarRenderer = null;
        private Xamarin.Forms.View NavBar => BitNavigationPage.GetNavBar(page: NavPage?.CurrentPage);
        private BitNavigationPage NavPage => Element as BitNavigationPage;

        protected override void OnElementChanged(ElementChangedEventArgs<NavigationPage> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement != null)
            {
                if (e.NewElement.CurrentPage != null)
                {
                    CurrentPageChanged();
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == nameof(NavigationPage.CurrentPage))
            {
                CurrentPageChanged();
            }
        }

        private void CurrentPageChanged()
        {
            if (_navBarRenderer != null)
            {
                ToolBar.RemoveView(_navBarRenderer.View);
                _navBarRenderer.Dispose();
                _navBarRenderer = null;
            }

            if (NavBar == null)
                return;

            _navBarRenderer = Platform.GetRenderer(NavBar);

            if (_navBarRenderer == null)
            {
                _navBarRenderer = Platform.CreateRendererWithContext(NavBar, Context);
                Platform.SetRenderer(NavBar, _navBarRenderer);
            }

            ToolBar.AddView(_navBarRenderer.View);

            if (NavBar.BindingContext == null && NavPage?.CurrentPage?.BindingContext != null)
            {
                NavBar.BindingContext = NavPage.CurrentPage.BindingContext;
            }
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            if (_navBarRenderer != null)
            {
                Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(_navBarRenderer.Element, new Rectangle(0, 0, Context.FromPixels(r - l), Context.FromPixels(ToolBar.MeasuredHeight)));
                _navBarRenderer.UpdateLayout();
            }
        }

        private Toolbar ToolBar
        {
            get
            {
                for (int i = 0; i < ChildCount; i++)
                {
                    if (GetChildAt(i) is Toolbar)
                    {
                        return GetChildAt(i) as Toolbar;
                    }
                }

                return null;
            }
        }
    }
}
#endif
