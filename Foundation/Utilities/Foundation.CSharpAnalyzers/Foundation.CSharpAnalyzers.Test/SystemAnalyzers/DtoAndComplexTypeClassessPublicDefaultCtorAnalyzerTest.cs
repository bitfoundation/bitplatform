using Foundation.CSharpAnalyzers.SystemAnalyzers;
using Foundation.CSharpAnalyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Foundation.CSharpAnalyzers.Test.SystemAnalyzers
{
    [TestClass]
    public class DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerTest : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindDtoAndComplexClassPublicDefaultCtor()
        {
            const string sourceCodeWithDtoAndComplexClassPublicDefaultCtor = @"

                public class ClassWithPublicCtorWhichIsNotValidDueHavingParameter
                {
                   public ClassWithPublicCtorWhichIsNotValidDueHavingParameter(int parameter)
                   {
                   }
                 }

                public class ClassWithPublicDefaultCtorWhichIsValidEvenWhenThereAreOtherCtorsDefined
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

                public class ClassWithoutAnyCtorWhichIsValid
                {
                }

                public class ClassWithCtorWithoutParameterWhichIsNotValid
                {
                    ClassWithCtorWithoutParameterWhichIsNotValid()
                    {
                    }
                 }

                public class ClassWithCtorWithoutParameterWhichIsNotValid2
                {
                    private ClassWithCtorWithoutParameterWhichIsNotValid2()
                    {
                    }
                 }";

            DiagnosticResult[] errors = new DiagnosticResult[] {
                 new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerMessage,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation("Test0.cs", 3, 17) }
                 },
                    new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerMessage,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation("Test0.cs", 29, 17) }
                 },
                    new DiagnosticResult {
                        Id = nameof(DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer),
                        Message = FoundationAnalyzersResources.DtoAndComplexTypeClassessPublicDefaultCtorAnalyzerMessage,
                        Severity = DiagnosticSeverity.Error,
                        Locations = new[] { new DiagnosticResultLocation("Test0.cs", 36, 17) }
                 }
            };

            VerifyCSharpDiagnostic(sourceCodeWithDtoAndComplexClassPublicDefaultCtor, errors);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DtoAndComplexTypeClassessPublicDefaultCtorAnalyzer();
        }
    }
}
