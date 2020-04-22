using Bit.Tooling.Core.Model;
using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Test.Extensions
{
    [TestClass]
    public class ITypeSymbolExtensionsTests : CodeGeneratorTest
    {
        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldDetectUnderlyingTypeSymbolAsDesired()
        {
            string dtoCode = @"
public interface IDto {
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual int? Id2 {get;set;}
    public virtual Task<int> Id3 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();

            Assert.AreEqual("Int32", dto.Properties.ElementAt(0).Type.GetUnderlyingTypeSymbol().Name);
            Assert.AreEqual("Int32", dto.Properties.ElementAt(1).Type.GetUnderlyingTypeSymbol().Name);
            Assert.AreEqual("Int32", dto.Properties.ElementAt(2).Type.GetUnderlyingTypeSymbol().Name);
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldDetectVoidReturnTypeAsDesired()
        {
            string controllerCode = @"

public interface IDto{
}

public class SampleDto:IDto{
}

public class SamplesController : DtoController<SampleDto> {

    [ActionAttribute]
    public void Do(){
    }

    [ActionAttribute]
    public Task Do2(){
    }

    [ActionAttribute]
    public int Do3(){
        return 0;
    }

    [ActionAttribute]
    public async Task<int> Do4(){
        return 0;
    }
}

public class DtoController<TDto>
    where TDto : IDto
{

}

";
            DefaultProjectDtoControllersProvider controllersProvider = new DefaultProjectDtoControllersProvider();

            DtoController controller = (await controllersProvider.GetProjectDtoControllersWithTheirOperations(CreateProjectFromSourceCodes(controllerCode))).Single();

            Assert.IsTrue(controller.Operations.ElementAt(0).ReturnType.IsVoid());
            Assert.IsTrue(controller.Operations.ElementAt(1).ReturnType.IsVoid());
            Assert.IsFalse(controller.Operations.ElementAt(2).ReturnType.IsVoid());
            Assert.IsFalse(controller.Operations.ElementAt(3).ReturnType.IsVoid());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldDetectIsCollectionTypeAsDesired()
        {
            string dtoCode = @"

public interface IDto {
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual string Id2 {get;set;}
    public virtual Task<string> Id3 {get;set;}
    public virtual int? Id4 {get;set;}
    public virtual IEnumerable<int> Id5 {get;set;}
    public virtual int[] Id6 {get;set;}
    public virtual System.Linq.IQueryable<int> Id7 {get;set;}
    public virtual System.Threading.Tasks.Task<int[]> Id8 {get;set;}
    public virtual System.Threading.Tasks.Task<System.Linq.IQueryable<int>> Id9 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";

            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();
            Assert.IsFalse(dto.Properties.ElementAt(0).Type.IsCollectionType());
            Assert.IsFalse(dto.Properties.ElementAt(1).Type.IsCollectionType());
            Assert.IsFalse(dto.Properties.ElementAt(2).Type.IsCollectionType());
            Assert.IsFalse(dto.Properties.ElementAt(3).Type.IsCollectionType());
            Assert.IsTrue(dto.Properties.ElementAt(4).Type.IsCollectionType());
            Assert.IsTrue(dto.Properties.ElementAt(5).Type.IsCollectionType());
            Assert.IsTrue(dto.Properties.ElementAt(6).Type.IsCollectionType());
            Assert.IsTrue(dto.Properties.ElementAt(7).Type.IsCollectionType());
            Assert.IsTrue(dto.Properties.ElementAt(8).Type.IsCollectionType());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldDetectIsQueryableTypeAsDesired()
        {
            string dtoCode = @"
public interface IDto {
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual string Id2 {get;set;}
    public virtual Task<string> Id3 {get;set;}
    public virtual int? Id4 {get;set;}
    public virtual IEnumerable<int> Id5 {get;set;}
    public virtual int[] Id6 {get;set;}
    public virtual System.Linq.IQueryable<int> Id7 {get;set;}
    public virtual System.Threading.Tasks.Task<int[]> Id8 {get;set;}
    public virtual System.Threading.Tasks.Task<System.Linq.IQueryable<int>> Id9 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";

            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();
            Assert.IsFalse(dto.Properties.ElementAt(0).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(1).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(2).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(3).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(4).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(5).Type.IsQueryableType());
            Assert.IsTrue(dto.Properties.ElementAt(6).Type.IsQueryableType());
            Assert.IsFalse(dto.Properties.ElementAt(7).Type.IsQueryableType());
            Assert.IsTrue(dto.Properties.ElementAt(8).Type.IsQueryableType());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldDetectNullableTypeSymbolAsDesired()
        {
            string dtoCode = @"
public interface IDto {
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual int? Id2 {get;set;}
    public virtual string Id3 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();

            Assert.IsFalse(dto.Properties.ElementAt(0).Type.IsNullable());
            Assert.IsTrue(dto.Properties.ElementAt(1).Type.IsNullable());
            Assert.IsTrue(dto.Properties.ElementAt(2).Type.IsNullable());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldReturnEdmElementTypeNameAsDesired()
        {
            string dtoCode = @"

public interface IDto {
}

public class SampleDto : IDto {
    public virtual System.Linq.IQueryable<int> Id {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();

            Assert.AreEqual("Edm.Int32", (dto.Properties.ElementAt(0).Type as INamedTypeSymbol).GetEdmElementTypeName());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldReturnEdmTypeNameAsDesired()
        {
            string dtoCode = @"
public interface IDto {
}

public class Person : IDto {
    
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual string Id2 {get;set;}
    public virtual Task<string> Id3 {get;set;}
    public virtual int? Id4 {get;set;}
    public virtual IEnumerable<int> Id5 {get;set;}
    public virtual System.Linq.IQueryable<int> Id6 {get;set;}
    public virtual System.Threading.Tasks.Task<int[]> Id7 {get;set;}
    public virtual System.Threading.Tasks.Task<System.Linq.IQueryable<int>> Id8 {get;set;}
    public virtual Person Id9 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Last();

            Assert.AreEqual("Edm.Int32", (dto.Properties.ElementAt(0).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Edm.String", (dto.Properties.ElementAt(1).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Edm.String", (dto.Properties.ElementAt(2).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Edm.Int32", (dto.Properties.ElementAt(3).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array", (dto.Properties.ElementAt(4).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array", (dto.Properties.ElementAt(5).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array", (dto.Properties.ElementAt(6).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array", (dto.Properties.ElementAt(7).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Person", (dto.Properties.ElementAt(8).Type as INamedTypeSymbol).GetEdmTypeName(useArrayForIEnumerableTypes: true));
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldReturnTypeScriptElementTypeNameAsDesired()
        {
            string dtoCode = @"

public interface IDto {
}

public class SampleDto : IDto {
    public virtual System.Linq.IQueryable<int> Id {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();

            Assert.AreEqual("number", (dto.Properties.ElementAt(0).Type as INamedTypeSymbol).GetTypeScriptElementTypeName());
        }

        [TestMethod]
        public async Task ITypeSymbolExtensionsShouldReturnTypeScriptTypeNameAsDesired()
        {
            string dtoCode = @"

public interface IDto {
}

public class Person : IDto {
    
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual string Id2 {get;set;}
    public virtual Task<string> Id3 {get;set;}
    public virtual int? Id4 {get;set;}
    public virtual IEnumerable<int> Id5 {get;set;}
    public virtual System.Linq.IQueryable<int> Id6 {get;set;}
    public virtual System.Threading.Tasks.Task<System.Linq.IQueryable<int>> Id7 {get;set;}
    public virtual Person Id8 {get;set;}
}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class SamplesController : DtoController<SampleDto>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Last();

            Assert.AreEqual("number", (dto.Properties.ElementAt(0).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("string", (dto.Properties.ElementAt(1).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("string", (dto.Properties.ElementAt(2).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("number", (dto.Properties.ElementAt(3).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array<number>", (dto.Properties.ElementAt(4).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array<number>", (dto.Properties.ElementAt(5).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Array<number>", (dto.Properties.ElementAt(6).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
            Assert.AreEqual("Person", (dto.Properties.ElementAt(7).Type as INamedTypeSymbol).GetTypescriptTypeName(useArrayForIEnumerableTypes: true));
        }

        [TestMethod]
        public virtual async Task ITypeSymbolExtensionsShouldFindDtosAsDesired()
        {
            string dtoCode = @"
public interface IDto {

}

public interface ISyncableDto : IDto {
}

public class Person : IDto {
    
}

public class Person2 : ISyncableDto {

}

public class Person3 : IDto , ISyncableDto {

}

public class Person4 {

}

public class DtoController<TDto>
    where TDto : IDto
{

}

public class PeopleController : DtoController<Person>
{

}

public class People2Controller : DtoController<Person2>
{

}

public class People3Controller : DtoController<Person3>
{

}

";
            DefaultProjectDtosProvider dtosProvider = new DefaultProjectDtosProvider(new DefaultProjectDtoControllersProvider());

            Dto[] dtos = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).ToArray();

            Assert.IsTrue(new[] { "Person3", "Person2", "Person" }.SequenceEqual(dtos.Select(d => d.DtoSymbol.Name).ToArray()));
        }
    }
}
