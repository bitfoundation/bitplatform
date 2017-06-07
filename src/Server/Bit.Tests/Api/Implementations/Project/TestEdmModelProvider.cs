using System;
using System.Web.OData.Builder;
using Bit.Api.Contracts.Project;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Core;

namespace Bit.Tests.Api.Implementations.Project
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
            _autoEdmBuilder.AutoBuildEdmFromAssembly(AssemblyContainer.Current.GetBitTestsAssembly(), edmModelBuilder);
        }

        public virtual string GetEdmName()
        {
            return "Test";
        }
    }
}
