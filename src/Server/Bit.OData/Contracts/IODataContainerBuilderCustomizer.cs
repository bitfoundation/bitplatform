using Microsoft.OData;

namespace Bit.OData.Contracts
{
    public interface IODataContainerBuilderCustomizer
    {
        void Customize(IContainerBuilder container);
    }
}
