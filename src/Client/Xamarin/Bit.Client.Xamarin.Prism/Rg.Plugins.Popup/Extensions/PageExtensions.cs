using System;
using Xamarin.Forms;
using System.Reflection;

namespace Prism.Plugin.Popups.Extensions
{
    public static class PageExtensions
    {
        public static bool IsOrDerivesFrom<T>(this Page page) where T : Page =>
            page is T || page.GetType().GetTypeInfo().IsSubclassOf(typeof(T));

        public static Page GetDisplayedPage(this Page page)
        {
            while (page.IsOrDerivesFrom<MasterDetailPage>() ||
                  page.IsOrDerivesFrom<TabbedPage>() ||
                  page.IsOrDerivesFrom<NavigationPage>())
            {
                switch (page)
                {
                    case MasterDetailPage mdp:
                        page = mdp.Detail;
                        break;
                    case TabbedPage tabbed:
                        page = tabbed.CurrentPage;
                        break;
                    case NavigationPage nav:
                        page = nav.CurrentPage;
                        break;
                }
            }

            return page;
        }
    }
}
