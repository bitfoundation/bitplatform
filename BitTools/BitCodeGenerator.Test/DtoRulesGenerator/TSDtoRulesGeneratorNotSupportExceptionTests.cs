using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitCodeGenerator.Test.Helpers;
using BitTools.Core.Model;
using BitCodeGenerator.Implementations;

namespace BitCodeGenerator.Test.DtoRulesGenerator
{
    [TestClass]
    public class TSDtoRulesGeneratorNotSupportExceptionTests : CodeGeneratorTest
    {
        public void TestNotSupported(string testCode, string message)
        {
            Project proj = CreateProjectFromSourceCodes(Codes.BitDtoRules, testCode);

            DtoRules productDtoRules = new DefaultProjectDtoRulesProvider().GetProjectAllDtoRules(proj).Single();

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
        public void TSDtoRulesGeneratorDoesNotSupportMultipleConstructors()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Multiple constructor is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportMethodOverloading()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Method overloading is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportRefParameters()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Ref parameter is not supported");
        }

        [TestMethod, TestCategory("TSDtoRuleGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportOutParameters()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Out parameter is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportAnonymousMethod()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Anonymous method expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportCast()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Cast expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportChecked()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Checked statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportDestructor()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Destructor declaration is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportEventDeclaration()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Event field declaration is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportGoTo()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Go to is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportLabeledStatement()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Labeled statements is not supported");
        }


        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportLock()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Lock statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportQuerySyntaxLinq()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Query syntax linq is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportRegion()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Region directive is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportSizeOf()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "SizeOf is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportTypeOf()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Type Of expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportUnsafe()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Unsafe statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportYield()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Yield statement is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportAwait()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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
            TestNotSupported(testCode, "Await expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportNullConditionalOperator()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Null-Conditional operator expression is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportVolatileKeyword()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Volatile keyword is not supported");
        }

        [TestMethod, TestCategory("TSDtoRulesGenerator")]
        public void TSDtoRulesGeneratorDoesNotSupportCatchBlockForInheritedExceptionTypes()
        {
            string testCode = @"

    using Foundation.Api.DtoRules;
    using System;

    namespace Test.Model.Dto
    {
        public class ProductDto : Foundation.Model.Dto.IDto
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

            TestNotSupported(testCode, "Catch block for inherited exception types is not supported");
        }
    }
}
