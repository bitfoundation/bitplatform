using Microsoft.OData.UriParser;

namespace Bit.OData.Implementations
{
    public class DefaultODataUriResolver : UnqualifiedODataUriResolver
    {
        public override bool EnableCaseInsensitive
        {
            get { return true; }
            set { }
        }
    }
}
