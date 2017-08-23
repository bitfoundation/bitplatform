using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IODataModelBuilder
    {
        void BuildModel(ODataModelBuilder odataModelBuilder);

        string GetODataRoute();
    }
}