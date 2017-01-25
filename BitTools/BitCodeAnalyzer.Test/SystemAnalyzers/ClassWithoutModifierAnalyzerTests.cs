using BitCodeAnalyzer.SystemAnalyzers;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitCodeAnalyzer.Test.SystemAnalyzers
{
    [TestClass]
    public class ClassWithoutModifierAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindClassesWithoutModifier()
        {
            const string sourceCodeWithClassWithoutModifier = @"
    namespace MyNamespace
    {
        public class MyClass
        {   

        }

        class MyClass
        {   

        }
    }";
            DiagnosticResult classWithoutModifier = new DiagnosticResult
            {
                Id = nameof(ClassWithoutModifierAnalyzer),
                Message = ClassWithoutModifierAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(9, 9) }
            };

            VerifyCSharpDiagnostic(sourceCodeWithClassWithoutModifier, classWithoutModifier);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ClassWithoutModifierAnalyzer();
        }
    }
}