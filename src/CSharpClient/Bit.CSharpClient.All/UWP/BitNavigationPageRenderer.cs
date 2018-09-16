#if UWP

using Bit.UWP;
using Bit.View;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Xamarin.Forms;
using Xamarin.Forms.Platform.UWP;

[assembly: ExportRenderer(typeof(BitNavigationPage), typeof(BitNavigationPageRenderer))]

namespace Bit.UWP
{
    public class BitNavigationPageRenderer : NavigationPageRenderer
    {
        private IVisualElementRenderer _navBarRenderer;
        private Border _originalCommandBarContent;

        private Xamarin.Forms.View NavBar => BitNavigationPage.GetNavBar(page: NavPage?.CurrentPage);
        private BitNavigationPage NavPage => Element as BitNavigationPage;

        public BitNavigationPageRenderer()
        {
            ElementChanged += BitNavigationPageRenderer_ElementChanged;
        }

        private void BitNavigationPageRenderer_ElementChanged(object sender, VisualElementChangedEventArgs e)
        {
            if (e.NewElement != null)
            {
                e.NewElement.PropertyChanged += HandlePropertyChanged;

                CurrentPageChanged();
            }
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName)
            {
                CurrentPageChanged();
            }

            if ((NavPage?.CurrentPage != null && NavBar != null && ContainerElement != null) && (e.PropertyName == "Width" || e.PropertyName == "Height"))
                Layout.LayoutChildIntoBoundingRegion(NavBar, new Rectangle(0, 0, ContainerElement.Width, ContainerElement.Height));
        }

        private async void CurrentPageChanged()
        {
            await Task.Yield();

            CommandBar commandBar = (CommandBar)ContainerElement
                .GetType()
                .GetField("_commandBar", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(ContainerElement);

            if (commandBar.Content is Border border)
                _originalCommandBarContent = border;

            if (_navBarRenderer != null)
            {
                _navBarRenderer.Dispose();
                commandBar.Content = _originalCommandBarContent;
                _navBarRenderer = null;
            }

            if (NavBar == null)
                return;

            if (NavBar.BindingContext == null && NavPage?.CurrentPage?.BindingContext != null)
                NavBar.BindingContext = NavPage.CurrentPage.BindingContext;

            Layout.LayoutChildIntoBoundingRegion(NavBar, new Rectangle(0, 0, ContainerElement.Width, ContainerElement.Height));

            _navBarRenderer = Platform.CreateRenderer(NavBar);
            Platform.SetRenderer(NavBar, _navBarRenderer);

            commandBar.Content = _navBarRenderer.ContainerElement;
        }
    }
}

#endif
