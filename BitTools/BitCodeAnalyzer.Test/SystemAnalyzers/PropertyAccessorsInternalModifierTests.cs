using BitCodeAnalyzer.SystemAnalyzers;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitCodeAnalyzer.Test.SystemAnalyzers
{
    [TestClass]
    public class PropertyAccessorsInternalModifierTests : DiagnosticVerifier
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
                Message = PropertyAccessorsInternalModifierAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(7, 13) }
            };

            VerifyCSharpDiagnostic(sourceCodeOfPropertiesWithInternalModifiersOnTheirAccessors, classWithoutModifier);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new PropertyAccessorsInternalModifierAnalyzer();
        }
    }
}