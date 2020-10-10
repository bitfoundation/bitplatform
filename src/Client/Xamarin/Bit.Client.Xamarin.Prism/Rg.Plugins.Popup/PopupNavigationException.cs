using Prism.Navigation;
using Xamarin.Forms;

namespace Prism.Plugin.Popups
{
    public class PopupNavigationException : NavigationException
    {
        public const string RootPageHasNotBeenSet = "Popup Pages cannot be set before the Application.MainPage has been set. You must have a valid NavigationStack prior to navigating.";

        public PopupNavigationException(string message, Page page)
            : base(message, page)
        {

        }
    }
}
