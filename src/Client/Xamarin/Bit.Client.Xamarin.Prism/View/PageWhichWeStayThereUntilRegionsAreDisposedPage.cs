using Xamarin.Forms;

namespace Bit.Client.Xamarin.Prism.View
{
    /// <summary>
    /// See DefaultNavService and BitApplication's RegisterTypes
    /// Without this, it's almost impossible to reload a page which has one or more regions!
    /// </summary>
    internal class PageWhichWeStayThereUntilRegionsAreDisposedPage : ContentPage
    {

    }
}
