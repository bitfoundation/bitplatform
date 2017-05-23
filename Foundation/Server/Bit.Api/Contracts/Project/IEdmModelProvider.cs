using System.Web.OData.Builder;

namespace Foundation.Api.Contracts.Project
{
    public interface IEdmModelProvider
    {
        void BuildEdmModel(ODataModelBuilder edmModelBuilder);

        string GetEdmName();
    }
}