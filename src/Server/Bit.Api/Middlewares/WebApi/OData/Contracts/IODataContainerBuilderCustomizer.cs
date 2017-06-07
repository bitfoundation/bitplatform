using Microsoft.OData;

namespace Bit.Api.Middlewares.WebApi.OData.Contracts
{
    public interface IODataContainerBuilderCustomizer
    {
        void Customize(IContainerBuilder container);
    }
}
