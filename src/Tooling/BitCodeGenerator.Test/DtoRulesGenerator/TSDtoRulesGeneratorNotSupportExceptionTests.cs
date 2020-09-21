using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Test.Helpers;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeGenerator.Test.DtoRulesGenerator
{
    [TestClass]
    public class TSDtoRulesGeneratorNotSupportExceptionTests : CodeGeneratorTest
    {
        public async Task TestNotSupported(string testCode, string message)
        {
            Project proj = CreateProjectFromSourceCodes(Codes.BitDtoRules, testCode);

            DtoRules productDtoRules = (await new DefaultProjectDtoRulesProvider().GetProjectAllDtoRules(proj)).Single();

            try
            {
                new DefaultDtoRulesValidator().Validate(productDtoRules);
                Assert.Fail();
            }
            catch (NotSupportedException ex)
            {
                if (ex.Message != message)
                    Assert.Fail();
            }
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportMultipleConstructors()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public ProductDtoRules() {  }
            public ProductDtoRules( int id  ) {  }
        }
    }

";

            await TestNotSupported(testCode, "Multiple constructor is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportMethodOverloading()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test(){}
            public void Test(int id){}
        }
    }

";
            await TestNotSupported(testCode, "Method overloading is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportRefParameters()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test(ref int id){}
        }
    }

";
            await TestNotSupported(testCode, "Ref parameter is not supported");
        }

        [TestMethod, TestCategory("TSDtoRuleGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportOutParameters()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test(out int id){ id = 0; }
        }
    }

";
            await TestNotSupported(testCode, "Out parameter is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportAnonymousMethod()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                Action someDelegate = delegate ()
                {

                };              
            }
        }
    }

";
            await TestNotSupported(testCode, "Anonymous method expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportCast()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                object o = null;
                int i = (int)o;           
            }
        }
    }

";
            await TestNotSupported(testCode, "Cast expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportChecked()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                checked
                {

                }           
            }
        }
    }

";
            await TestNotSupported(testCode, "Checked statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportDestructor()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            ~ProductDtoRules()
            {

            }
        }
    }

";
            await TestNotSupported(testCode, "Destructor declaration is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportEventDeclaration()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public event Action Test;
        }
    }

";
            await TestNotSupported(testCode, "Event field declaration is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportGoTo()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                goto SomeWhere;

                SomeWhere:
                {

                }
            }
        }
    }

";

            await TestNotSupported(testCode, "Go to is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportLabeledStatement()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                SomeWhere:
                {

                }
            }
        }
    }

";
            await TestNotSupported(testCode, "Labeled statements is not supported");
        }


        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportLock()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                lock (this)
                {

                }
            }
        }
    }

";

            await TestNotSupported(testCode, "Lock statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportQuerySyntaxLinq()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                var q = from p in ""
                    select p;
            }
        }
    }

";

            await TestNotSupported(testCode, "Query syntax linq is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportRegion()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                #region R
                #endregion
            }
        }
    }

";

            await TestNotSupported(testCode, "Region directive is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportSizeOf()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                int shortSize = sizeof(short);
            }
        }
    }

";

            await TestNotSupported(testCode, "SizeOf is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportTypeOf()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                Type type = typeof(string);
            }
        }
    }

";

            await TestNotSupported(testCode, "Type Of expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportUnsafe()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                unsafe
                {

                }
            }
        }
    }

";
            await TestNotSupported(testCode, "Unsafe statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportYield()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public System.Collections.Generic.IEnumerable<string> Test()
            {
                yield return "";
            }
        }
    }

";
            await TestNotSupported(testCode, "Yield statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportAwait()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public async void Test()
            {
                await System.Threading.Tasks.Task.Delay(1);
            }
        }
    }

";
            await TestNotSupported(testCode, "Await expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportNullConditionalOperator()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                string test = ""test"";
                int? length = test?.Length;
            }
        }
    }

";

            await TestNotSupported(testCode, "Null-Conditional operator expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportVolatileKeyword()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            volatile int _Test;
        }
    }

";

            await TestNotSupported(testCode, "Volatile keyword is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public async Task TSDtoRulesGeneratorDoesNotSupportCatchBlockForInheritedExceptionTypes()
        {
            string testCode = @"

    using Bit.Owin.DtoRules;
    using System;

    namespace Test.Model.Dto
    {
        public class ProductDto : Bit.Model.Contracts.IDto
        {
            public virtual int Id { get; set; }
        }
    }

    namespace Test.Api.DtoRules
    {
        [AutoGenerate]
        public class ProductDtoRules : DtoRules<Model.Dto.ProductDto>
        {
            public void Test()
            {
                try { } catch (System.NotSupportedException ex) { }
            }
        }
    }

";

            await TestNotSupported(testCode, "Catch block for inherited exception types is not supported");
        }
    }
}
