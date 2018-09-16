#if iOS
using Bit.iOS;
using Bit.View;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(BitNavigationPage), typeof(BitNavigationPageRenderer))]

namespace Bit.iOS
{
    public class BitNavigationPageRenderer : NavigationRenderer
    {
        private IVisualElementRenderer _navBarRenderer;
        private Xamarin.Forms.View NavBar => BitNavigationPage.GetNavBar(page: NavPage?.CurrentPage);
        private BitNavigationPage NavPage => Element as BitNavigationPage;

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName)
            {
                CurrentPageChanged();
            }
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavPage.PropertyChanged += HandlePropertyChanged; // We've no OnElementPropertyChanged method to be overriden!

            CurrentPageChanged();
        }

        private void CurrentPageChanged()
        {
            if (_navBarRenderer != null)
            {
                _navBarRenderer.NativeView.RemoveFromSuperview();
                _navBarRenderer.Dispose();
                _navBarRenderer = null;
            }

            if (NavBar == null)
                return;

            _navBarRenderer = Platform.CreateRenderer(NavBar);
            Platform.SetRenderer(NavBar, _navBarRenderer);
            NavigationBar.AddSubview(_navBarRenderer.NativeView);
            View.SetNeedsLayout();

            if (NavBar.BindingContext == null && NavPage?.CurrentPage?.BindingContext != null)
            {
                NavBar.BindingContext = NavPage.CurrentPage.BindingContext;
            }
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();

            if (NavBar == null || _navBarRenderer == null)
                return;

            Thickness margin = NavBar.Margin;

            _navBarRenderer.NativeView.Frame = new CoreGraphics.CGRect(
                margin.Left,
                margin.Top,
                NavigationBar.Frame.Width - margin.Left - margin.Right,
                NavigationBar.Frame.Height - margin.Bottom - margin.Top);

            Layout.LayoutChildIntoBoundingRegion(NavBar, new Rectangle(0, 0, NavigationBar.Frame.Width, NavigationBar.Frame.Height));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (NavPage != null)
                NavPage.PropertyChanged -= HandlePropertyChanged;
        }
    }
}
#endif
