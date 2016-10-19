using System;
using System.Web.OData.Builder;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Core.Contracts.Project;
using Foundation.Api.Contracts.Project;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;

namespace Foundation.Test.Api.Implementations.Project
{
    public class TestEdmModelProvider : IEdmModelProvider
    {
        private readonly IAutoEdmBuilder _autoEdmBuilder;

        public TestEdmModelProvider(IAutoEdmBuilder autoEdmBuilder)
        {
            if (autoEdmBuilder == null)
                throw new ArgumentNullException(nameof(autoEdmBuilder));

            _autoEdmBuilder = autoEdmBuilder;
        }

        protected TestEdmModelProvider()
        {

        }

        public virtual void BuildEdmModel(ODataModelBuilder edmModelBuilder)
        {
            _autoEdmBuilder.AutoBuildEdmFromAssembly(typeof(TestEdmModelProvider).Assembly, edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "Test";
        }
    }
}
