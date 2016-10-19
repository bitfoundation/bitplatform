using Microsoft.OData;

namespace Foundation.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IODataContainerBuilderCustomizer
    {
        void Customize(IContainerBuilder container);
    }
}
