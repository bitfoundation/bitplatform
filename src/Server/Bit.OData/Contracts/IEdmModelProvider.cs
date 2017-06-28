using System.Web.OData.Builder;

namespace Bit.OData.Contracts
{
    public interface IEdmModelProvider
    {
        void BuildEdmModel(ODataModelBuilder edmModelBuilder);

        string GetEdmName();
    }
}