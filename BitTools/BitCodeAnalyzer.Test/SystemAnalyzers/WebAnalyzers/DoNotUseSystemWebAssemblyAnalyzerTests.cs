using System;
using System.Reflection;
using System.Web.Hosting;
using BitCodeAnalyzer.SystemAnalyzers.WebAnalyzers;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitCodeAnalyzer.Test.SystemAnalyzers.WebAnalyzers
{
    [TestClass]
    public class DoNotUseSystemWebAssemblyAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindSystemWebUsages()
        {
            typeof(HostingEnvironment).GetTypeInfo();

            const string sourceCodeWithSystemWebUsage = @"
    using System;

    namespace MyNamespace
    {
        class MyClass
        {   
            public MyClass()
            {
                bool isHosted = System.Web.Hosting.HostingEnvironment.IsHosted;
            }
        }
    }";
            DiagnosticResult firstSystemWebUsage = new DiagnosticResult
            {
                Id = nameof(DoNotUseSystemWebAssemblyAnalyzer),
                Message = DoNotUseSystemWebAssemblyAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(10, 52) }
            };

            VerifyCSharpDiagnostic(sourceCodeWithSystemWebUsage, firstSystemWebUsage);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DoNotUseSystemWebAssemblyAnalyzer();
        }
    }
}