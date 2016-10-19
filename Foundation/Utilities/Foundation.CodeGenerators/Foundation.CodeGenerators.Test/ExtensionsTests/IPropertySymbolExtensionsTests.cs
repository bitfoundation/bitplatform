using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Foundation.CodeGenerators.Test.ExtensionsTests
{
    [TestClass]
    public class IPropertySymbolExtensionsTests : CodeGeneratorTest
    {
        [TestMethod]
        public void IPropertySymbolExtensionsShouldDeterminateKeyPropertiesAsDesierd()
        {
            string dtoCode = @"

public interface IDto {
}

public class SampleDto : IDto {
    public virtual int Id {get;set;}
    public virtual int SampleDtoId {get;set;}
    [KeyAttribute]
    public virtual int SomeKye{get;set;}
    public virtual string NotAKey{get;set;}
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

            Dto dto = dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode)).Single();

            Assert.IsFalse(dto.Properties.ElementAt(0).IsKey());
            Assert.IsFalse(dto.Properties.ElementAt(1).IsKey());
            Assert.IsTrue(dto.Properties.ElementAt(2).IsKey());
            Assert.IsFalse(dto.Properties.ElementAt(3).IsKey());
        }
    }
}
