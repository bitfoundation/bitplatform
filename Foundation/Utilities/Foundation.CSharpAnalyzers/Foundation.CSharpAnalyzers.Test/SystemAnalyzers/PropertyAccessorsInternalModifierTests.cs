using Foundation.CSharpAnalyzers.SystemAnalyzers;
using Foundation.CSharpAnalyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Foundation.CSharpAnalyzers.Test.SystemAnalyzers
{
    [TestClass]
    public class PropertyAccessorsInternalModifierTests : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindPropertiesWithInternalModifiersOnTheirAccessors()
        {
            const string sourceCodeOfPropertiesWithInternalModifiersOnTheirAccessors = @"
    namespace MyNamespace
    {
        public class MyClass
        {   
            public int Id2 { get; set; }
            public int Id { get; internal set; }
        }
    }";
            DiagnosticResult classWithoutModifier = new DiagnosticResult
            {
                Id = nameof(PropertyAccessorsInternalModifierAnalyzer),
                Message = FoundationAnalyzersResources.PropertyAccessorsInternalModifierAnalyzerMessage,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 13) }
            };

            VerifyCSharpDiagnostic(sourceCodeOfPropertiesWithInternalModifiersOnTheirAccessors, classWithoutModifier);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PropertyAccessorsInternalModifierAnalyzer();
        }
    }
}