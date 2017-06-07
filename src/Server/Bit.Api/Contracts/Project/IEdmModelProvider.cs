using System.Web.OData.Builder;

namespace Bit.Api.Contracts.Project
{
    public interface IEdmModelProvider
    {
        void BuildEdmModel(ODataModelBuilder edmModelBuilder);

        string GetEdmName();
    }
}