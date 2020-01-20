using BitCodeGenerator.Implementations;
using BitCodeGenerator.Test.Helpers;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.Implementations
{
    [TestClass]
    public class DefaultProjectDtoControllersProviderTests : CodeGeneratorTest
    {
        [TestMethod]
        public virtual async Task DefaultProjectDtoControllersProviderShouldReturnDtoControllersAsDesired()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                IList<DtoController> controllers = await new DefaultProjectDtoControllersProvider()
                    .GetProjectDtoControllersWithTheirOperations(solution.Projects.Single(p => p.Name == "Bit.OData"));

                Assert.IsTrue(
                    controllers.Select(c => c.Name).SequenceEqual(new[] { "ClientsLogs", "JobsInfo", "UsersSettings" }));
            }
        }

        [TestMethod]
        public virtual async Task DefaultProjectDtoControllersProviderShouldReturnDtoControllersOfTestProjectAsDesired()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                IList<DtoController> controllers = await new DefaultProjectDtoControllersProvider()
                    .GetProjectDtoControllersWithTheirOperations(solution.Projects.Single(p => p.Name == "Bit.Tests"));

                Assert.AreEqual(12, controllers.Count);
            }
        }

        [TestMethod]
        public virtual async Task DefaultProjectDtoControllersProviderShouldFindODataOperationParametersCorrectly()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"
using System;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Bit.Tests.Model.DomainModels;
using Bit.OData.ODataControllers;

namespace Bit.OData.ODataControllers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ActionAttribute : Attribute
    {

    }
}

public interface IDto {
}

public class TestModel : IDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

[System.ComponentModel.DataAnnotations.Schema.ComplexType]
public class ComplexObj3
{
    public virtual string Name { get; set; }
}

namespace Bit.Tests.Api.ApiControllers
{
    public class TestController : DtoController<TestModel>
    {

        public class DoParameters
        {
            public virtual string Parameter1 { get; set; }
            public virtual string Parameter1 { get; set; }
        }

        [Action]
        public virtual async Task Do(DoParameters parameters)
        {

        }

        [Action]
        public virtual async System.Threading.Tasks.Task<int[]> Do1()
        {
        }

        public class Do3Parameters
        {
            public virtual int[] values { get; set; }
        }

        [Action]
        public virtual async System.Threading.Tasks.Task<ComplexObj3[]> Do3(Do3Parameters parameters)
        {
        }

        [Action]
        public virtual async System.Threading.Tasks.Task Do4(TestModel testModel)
        {
        }
    }
}
";

            IList<DtoController> controllers = await new DefaultProjectDtoControllersProvider()
                    .GetProjectDtoControllersWithTheirOperations(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter));

            Assert.AreEqual("Edm.Int32", controllers.Single()
                .Operations.ElementAt(1)
                .ReturnType.GetEdmElementTypeName());

            Assert.AreEqual("ComplexObj3", controllers.Single()
                .Operations.ElementAt(2)
                .ReturnType.GetEdmElementTypeName());

            Assert.AreEqual("Edm.Int32", controllers.Single()
                .Operations.ElementAt(2)
                .Parameters.Single().Type.GetEdmElementTypeName());

            Assert.AreEqual("TestModel", controllers.Single()
                .Operations.ElementAt(3)
                .Parameters.Single().Type.GetEdmTypeName(useArrayForIEnumerableTypes: false));
        }
    }
}
