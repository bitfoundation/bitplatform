using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IODataServiceBuilder
    {
        void BuildModel(ODataModelBuilder oDataModelBuilder);

        string GetODataRoute();
    }
}