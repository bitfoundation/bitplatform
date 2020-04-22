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
    public class ISymbolExtensionsTests : CodeGeneratorTest
    {
        [TestMethod]
        [Ignore]
        public async Task ISymbolExtensionsShouldReturnCommentsAsDesired()
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

            Dto dto = (await dtosProvider.GetProjectDtos(CreateProjectFromSourceCodes(dtoCode))).Single();

            Assert.AreEqual(@"/**
    Test
    */", dto.Properties.ElementAt(0).GetDocumentationSummary());

            Assert.AreEqual("", dto.Properties.ElementAt(1).GetDocumentationSummary());
        }
    }
}
