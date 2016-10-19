using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.CodeGenerators.Test.TSDtoRulesGenerator
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

            IList<DtoRules> AllDtoRules = new DefaultProjectDtoRulesProvider().GetProjectAllDtoRules(proj);

            Assert.AreEqual(2, AllDtoRules.Count);
            Assert.AreEqual("ProductDtoRules", AllDtoRules.ElementAt(0).Name);
            Assert.AreEqual("CategoryDtoRules", AllDtoRules.ElementAt(1).Name);
            Assert.AreEqual("ProductDto", AllDtoRules.ElementAt(0).DtoSymbol.Name);
            Assert.AreEqual("CategoryDto", AllDtoRules.ElementAt(1).DtoSymbol.Name);
            Assert.AreEqual("ProductDtoRules", AllDtoRules.ElementAt(0).DtoRulesSymbol.Name);
            Assert.AreEqual("CategoryDtoRules", AllDtoRules.ElementAt(1).DtoRulesSymbol.Name);
        }
    }
}
