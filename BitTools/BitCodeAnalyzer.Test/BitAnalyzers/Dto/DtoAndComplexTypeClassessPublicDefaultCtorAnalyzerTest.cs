using BitCodeAnalyzer.BitAnalyzers.Dto;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitCodeAnalyzer.Test.BitAnalyzers.Dto
{
    [TestClass]
    public class DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerTest : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindDtoAndComplexClassPublicDefaultCtor()
        {
            const string sourceCodeWithDtoAndComplexClassPublicDefaultCtor = @"
                public class ClassWithPublicCtorWhichIsNotValidDueHavingParameter : IDto
                {
                   public ClassWithPublicCtorWhichIsNotValidDueHavingParameter(int parameter)
                   {
                   }
                 }

                public class ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined : IDto
                {
                   public ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined()
                   {
                   }

                   internal ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined(int parameter)
                   {
                   }

                   public ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined(string parameter)
                   {
                   }
                 }

                public class ClassWithoutAnyCtorWhichIsValid : IDto
                {
                }

                public class ClassWithCtorWithoutParameterWhichIsNotValid : IDto
                {
                    ClassWithCtorWithoutParameterWhichIsNotValid()
                    {
                    }
                 }

                public class ClassWithCtorWithoutParameterWhichIsNotValid2 : IDto
                {
                    private ClassWithCtorWithoutParameterWhichIsNotValid2()
                    {
                    }
                 }

                public class ClassWithPublicCtorWhichIsNotValidDueHavingParameter2
                {
                   public ClassWithPublicCtorWhichIsNotValidDueHavingParameter2(int parameter)
                   {
                   }
                 }

                public class ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined2
                {
                   public ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined2()
                   {
                   }

                   internal ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined2(int parameter)
                   {
                   }

                   public ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined2(string parameter)
                   {
                   }
                 }

                public class ClassWithoutAnyCtorWhichIsValid2
                {
                }

                public class ClassWithCtorWithoutParameterWhichIsNotValid22
                {
                    ClassWithCtorWithoutParameterWhichIsNotValid22()
                    {
                    }
                 }

                public class ClassWithCtorWithoutParameterWhichIsNotValid22
                {
                    private ClassWithCtorWithoutParameterWhichIsNotValid22()
                    {
                    }
                 }

                public interface IDto {
                }";

            DiagnosticResult[] errors = new DiagnosticResult[] {
                 new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer.Message,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation(2, 17) }
                 },
                    new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer.Message,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation(28, 17) }
                 },
                    new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer.Message,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation(35, 17) }
                 }
            };

            VerifyCSharpDiagnostic(sourceCodeWithDtoAndComplexClassPublicDefaultCtor, errors);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer();
        }
    }
}
