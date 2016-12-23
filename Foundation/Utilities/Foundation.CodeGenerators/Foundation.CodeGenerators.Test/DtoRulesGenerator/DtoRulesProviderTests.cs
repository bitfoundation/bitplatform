using System.Collections.Generic;
using System.Linq;
using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.CodeGenerators.Test.DtoRulesGenerator
{
    [TestClass]
    public class DtoRulesProviderTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorShouldFindDtoRules()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
        {
            public virtual int Id { get; set; }
        }

        public class CategoryDto : Foundation.Model.Dto.IDto
        {
            public virtual int Id { get; set; }
        }

        public class CustomerDto : Foundation.Model.Dto.IDto
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

        [Foundation.Api.DtoRules.AutoGenerate]
        public class CategoryDtoRules : Foundation.Api.DtoRules.DtoRules<Model.Dto.CategoryDto>
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
            Project proj = CreateProjectFromSourceCodes(Codes.FoundationDtoRules, testCode);

            IList<DtoRules> allDtoRules = new DefaultProjectDtoRulesProvider().GetProjectAllDtoRules(proj);

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
