using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Test.Helpers;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.DtoRulesGenerator
{
    [TestClass]
    public class DtoRulesProviderTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorShouldFindDtoRules()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }

        public class CategoryDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }

        public class CustomerDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {

        }

        [Bit.Owin.DtoRules.AutoGenerate]
        public class CategoryDtoRules : Bit.Owin.DtoRules.DtoRules<Model.Dto.CategoryDto>
        {

        }

        public class CustomerDtoRules : DtoRules<Model.Dto.CustomerDto>
        {

        }

        public class XDtoRules
        {

        }

        public class Fake
        {

        }
    }

";
            Project proj = CreateProjectFromSourceCodes(Codes.BitDtoRules, testCode);

            IList<DtoRules> allDtoRules = await new DefaultProjectDtoRulesProvider().GetProjectAllDtoRules(proj);

            Assert.AreEqual(2, allDtoRules.Count);
            Assert.AreEqual("ProductDtoRules", allDtoRules.ElementAt(0).Name);
            Assert.AreEqual("CategoryDtoRules", allDtoRules.ElementAt(1).Name);
            Assert.AreEqual("ProductDto", allDtoRules.ElementAt(0).DtoSymbol.Name);
            Assert.AreEqual("CategoryDto", allDtoRules.ElementAt(1).DtoSymbol.Name);
            Assert.AreEqual("ProductDtoRules", allDtoRules.ElementAt(0).DtoRulesSymbol.Name);
            Assert.AreEqual("CategoryDtoRules", allDtoRules.ElementAt(1).DtoRulesSymbol.Name);
        }
    }
}
