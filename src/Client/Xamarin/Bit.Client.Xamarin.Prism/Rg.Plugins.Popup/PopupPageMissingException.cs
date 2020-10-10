using System;
namespace Prism.Plugin.Popups
{
    public class PopupPageMissingException : Exception
    {
        public PopupPageMissingException(string pageName, Exception innerException = null)
            : base($"There seems to be an issue resolving the PopupPage '{pageName}'. Either we couldn't find it or we did, but we got something other than a Popup Page.", innerException)
        {
        }
    }
}
