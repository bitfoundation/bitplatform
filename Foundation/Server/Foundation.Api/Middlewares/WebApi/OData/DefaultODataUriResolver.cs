using Microsoft.OData.UriParser;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    public class DefaultODataUriResolver : ODataUriResolver
    {
        public DefaultODataUriResolver()
        {

        }

        public override bool EnableCaseInsensitive
        {
            get { return true; }
            set { }
        }

    }
}
