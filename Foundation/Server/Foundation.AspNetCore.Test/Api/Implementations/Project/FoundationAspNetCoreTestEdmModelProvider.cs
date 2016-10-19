using Foundation.Api.Contracts.Project;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using System;
using System.Reflection;
using System.Web.OData.Builder;

namespace Foundation.AspNetCore.Test.Api.Implementations.Project
{
    public class FoundationAspNetCoreTestEdmModelProvider : IEdmModelProvider
    {
        private readonly IAutoEdmBuilder _autoEdmBuilder;

        public FoundationAspNetCoreTestEdmModelProvider(IAutoEdmBuilder autoEdmBuilder)
        {
            if (autoEdmBuilder == null)
                throw new ArgumentNullException(nameof(autoEdmBuilder));

            _autoEdmBuilder = autoEdmBuilder;
        }

        protected FoundationAspNetCoreTestEdmModelProvider()
        {

        }

        public virtual void BuildEdmModel(ODataModelBuilder edmModelBuilder)
        {
            _autoEdmBuilder.AutoBuildEdmFromAssembly(typeof(FoundationAspNetCoreTestEdmModelProvider).GetTypeInfo().Assembly, edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "AspNetCore";
        }
    }
}
