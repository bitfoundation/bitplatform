using Microsoft.AspNet.OData;

namespace Bit.OData.Implementations
{
    public class DefaultODataUriResolver : UnqualifiedCallAndEnumPrefixFreeResolver
    {
        public override bool EnableCaseInsensitive
        {
            get => true;
            set { }
        }
    }
}
