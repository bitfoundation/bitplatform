using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Foundation.CodeGenerators.Test.ExtensionsTests
{
    [TestClass]
    public class ISymbolExtensionsTests : CodeGeneratorTest
    {
        [TestMethod]
        public void ISymbolExtensionsShouldReturnCommentsAsDesired()
        {
            string dtoCode = @"

public interface IDto {
}

public class SampleDto : IDto {
    /// <summary>
    /// Test
    /// </summary>
    public virtual int Id {get;set;}
    public virtual string Test {get;set;}
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

            Assert.AreEqual(@"/**
    Test
    */", dto.Properties.ElementAt(0).GetDocumentationSummary());

            Assert.AreEqual("", dto.Properties.ElementAt(1).GetDocumentationSummary());
        }
    }
}
