using BitCodeGenerator.Implementations;
using BitCodeGenerator.Test.Helpers;
using BitTools.Core.Contracts;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.Implementations
{
    [TestClass]
    public class DefaultProjectDtosProviderTests : CodeGeneratorTest
    {
        [TestMethod]
        public virtual async Task DefaultProjectDtosProviderShouldReturnDtosAsDesired()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

                IList<Dto> dtos = (await projectDtosProvider.GetProjectDtos(solution.Projects.Single(p => p.Name == "Bit.OData"))).ToList();

                Assert.IsTrue(dtos.Select(d => d.DtoSymbol.Name).SequenceEqual(new[] { "UserSetting", "JobInfoDto", "ClientLogDto" }));
            }
        }

        [TestMethod]
        public virtual async Task DefaultProjectDtosProviderShouldReturnDtosOfTestProjectAsDesired()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

                IList<Dto> dtos = (await projectDtosProvider.GetProjectDtos(solution.Projects.Single(p => p.Name == "Bit.Tests"))).ToList();

                Assert.AreEqual(14, dtos.Count);
            }
        }

        [TestMethod]
        public virtual async Task ISyncableDtoShouldHaveIsSyncedPropertyEvenWhenThereIsNoDeclaredIsSyncedProperty()
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

            IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(1, (await projectDtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter))).Single().Properties.Count);
        }

        [TestMethod]
        public virtual async Task ISyncableDtoShouldHaveIsSyncedProperty()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"

public interface IDto {
}

public interface ISyncableDto : IDto {
}

public class TestDto : ISyncableDto {
    public bool IsSynced { get; set; }
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class TestController : DtoController<TestDto>
{

}
";

            IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(1, (await projectDtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter))).Single().Properties.Count);
        }

        [TestMethod]
        public virtual async Task IDtoShouldNotHaveIsSyncedPropertyWhenItIsNotISyncable()
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

            IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Assert.AreEqual(0, (await projectDtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodeOfDtoControllerWithActionAndParameter))).Single().Properties.Count);
        }

        [TestMethod]
        public virtual async Task DefaultDtosProviderShouldReturnComplexTypesOfSourceProjectsOnly()
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
    [FunctionAttribute]
    public System.Threading.Tasks.Task<ComplexObj3[]> GetComplexObjects()
    {
        return null;
    }
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
}

[System.ComponentModel.DataAnnotations.Schema.ComplexType]
public class ComplexObj3
{
    public virtual string Name { get; set; }
}

";

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

            DefaultProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto[] dtos = (await projectDtosProvider.GetProjectDtos(sourceProject)).ToArray();

            Assert.IsTrue(dtos.Select(d => d.DtoSymbol.Name).SequenceEqual(new[] { "ComplexObj3", "ComplexObj", "TestComplexDto" }));
        }

        [TestMethod]
        public virtual async Task DefaultDtosProviderShouldReturnDtosInOrdered()
        {
            const string sourceCodes = @"

public interface IDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class XDto : CityDto
{
    public CustomerDto Customer { get; set; }
}

public class PersonDto : IDto
{
    public int Id { get; set; }
}

public class CustomerDto : PersonDto
{
    public CityDto City { get; set; }
}

public class CityDto : IDto
{
    public int Id { get; set; }
}

public class PeopleController : DtoController<PersonDto>
{
}

public class CustomersController : DtoController<CustomerDto>
{
}

public class CitiesController : DtoController<CityDto>
{
}

public class XController : DtoController<XDto>
{
}

";

            IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            IList<Dto> result = await projectDtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodes));

            Assert.IsTrue(result.Select(dto => dto.DtoSymbol.Name).SequenceEqual(new[] { "CityDto", "XDto", "PersonDto", "CustomerDto" }));
        }

        [TestMethod]
        public virtual async Task DefaultDtosProviderShouldReturnDtosOfGenericDesign()
        {
            const string sourceCodes = @"

public interface IDto {
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class PersonDto : IDto
{
    public int Id { get; set; }
}

public class CustomerDto : PersonDto
{
    
}

public class EmployeeDto : PersonDto
{
    
}

public class PeopleController<TPersonDto> : DtoController<TPersonDto>
    where TPersonDto : PersonDto
{
}

public class CustomersController : PeopleController<CustomerDto>
{
}

public class EmployeesController : PeopleController<EmployeeDto>
{
}

";
            IProjectDtosProvider projectDtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            IList<Dto> result = await projectDtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(sourceCodes));

            Assert.IsTrue(result.Select(dto => dto.DtoSymbol.Name).SequenceEqual(new[] { "PersonDto", "EmployeeDto", "CustomerDto" }));
        }
    }
}
