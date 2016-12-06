using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Foundation.CodeGenerators.Contracts;

namespace Foundation.CodeGenerators.Test
{
    [TestClass]
    public class DefaultProjectDtosProviderTests : CodeGeneratorTest
    {
        [TestMethod]
        public virtual async Task DefaultProjectDtosProviderShouldReturnDtosAsDesired()
        {
            using (Workspace workspace = GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

                IList<Dto> dtos = dtosProvider.GetProjectDtos(solution.Projects.Single(p => p.Name == "Foundation.Model"))
                    .Union(dtosProvider.GetProjectDtos(solution.Projects.Single(p => p.Name == "Foundation.Api"))).ToList();

                Assert.IsTrue(dtos.Select(d => d.DtoSymbol.Name).SequenceEqual(new[] { "ClientLogDto", "JobInfo", "UserSetting" }));
            }
        }

        [TestMethod]
        public virtual async Task ISyncableDtoShouldHaveISVPropertyEvenWhenThereIsNoDeclaredISVProperty()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"

public interface IDto {
}

public interface ISyncableDto : IDto {
}

public class TestDto : ISyncableDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class TestController : DtoController<TestDto>
{

}
";

            IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(1, dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter)).Single().Properties.Count);
        }

        [TestMethod]
        public virtual async Task ISyncableDtoShouldHaveISVProperty()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"

public interface IDto {
}

public interface ISyncableDto : IDto {
}

public class TestDto : ISyncableDto {
    public bool ISV { get; set; }
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class TestController : DtoController<TestDto>
{

}
";

            IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(1, dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter)).Single().Properties.Count);
        }

        [TestMethod]
        public virtual async Task IDtoShouldNotHaveISVPropertyWhenItIsNotISyncable()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"

public interface IDto {
}

public interface ISyncableDto : IDto {
}

public class TestDto : IDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class TestController : DtoController<TestDto>
{

}
";

            IProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(0, dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter)).Single().Properties.Count);
        }

        [TestMethod]
        public virtual void DefaultDtosProviderShouldReturnComplexTypesOfSourceProjectsOnly()
        {
            string sourceProjectCodes = @"

public interface IDto {

}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class TestComplexController : DtoController<TestComplexDto>
{

}

public class TestComplexDto : IDto
{
    public virtual int EntityId { get; set; }

    public virtual ComplexObj ComplexObj { get; set; }

    public virtual ComplexObj2 ComplexObj2 { get; set; }
}

[System.ComponentModel.DataAnnotations.Schema.ComplexType]
public class ComplexObj
{
    public virtual string Name { get; set; }
}";

            string otherProjectCodes = @"

namespace System.ComponentModel.DataAnnotations.Schema
{
    public class ComplexTypeAttribute : Attribute
    {
    }
}

[System.ComponentModel.DataAnnotations.Schema.ComplexType]
public class ComplexObj2
{
    public virtual string Name { get; set; }
}

";

            Project otherProject = CreateProjectFromSourceCodes(otherProjectCodes);
            Project sourceProject = CreateProjectFromSourceCodesWithExistingSolution(otherProject.Solution, sourceProjectCodes);
            sourceProject = sourceProject.AddProjectReference(new ProjectReference(otherProject.Id));

            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto[] dtos = dtosProvider.GetProjectDtos(sourceProject).ToArray();

            Assert.IsTrue(dtos.Select(d => d.DtoSymbol.Name).SequenceEqual(new[] { "TestComplexDto", "ComplexObj" }));
        }
    }
}
