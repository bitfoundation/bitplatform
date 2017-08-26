using System;
using System.Web.OData.Builder;
using Bit.Core;
using Bit.OData.Contracts;

namespace Bit.Tests.Api.Implementations.Project
{
    public class TestODataServiceBuilder : IODataServiceBuilder
    {
        private readonly IAutoODataModelBuilder _autoODataModelBuilder;

        public TestODataServiceBuilder(IAutoODataModelBuilder autoODataModelBuilder)
        {
            if (autoODataModelBuilder == null)
                throw new ArgumentNullException(nameof(autoODataModelBuilder));

            _autoODataModelBuilder = autoODataModelBuilder;
        }

        protected TestODataServiceBuilder()
        {

        }

        public virtual void BuildModel(ODataModelBuilder edmModelBuilder)
        {
            _autoODataModelBuilder.AutoBuildODatModelFromAssembly(AssemblyContainer.Current.GetBitTestsAssembly(), edmModelBuilder);
        }

        public virtual string GetODataRoute()
        {
            return "Test";
        }
    }
}
