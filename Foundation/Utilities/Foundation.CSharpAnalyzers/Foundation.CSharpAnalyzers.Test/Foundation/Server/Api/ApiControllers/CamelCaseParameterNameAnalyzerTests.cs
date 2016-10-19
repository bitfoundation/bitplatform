using Foundation.CSharpAnalyzers.Foundation.Server.Api.ApiControllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Foundation.CSharpAnalyzers.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Foundation.CSharpAnalyzers.Test.Foundation.Server.Api.ApiControllers
{
    [TestClass]
    public class CamelCaseParameterNameAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void FindNonCamelCaseParameterNamesTest()
        {
            const string sourceCodeOfDtoControllerWithActionAndParameter = @"
    using System;
using System.Threading.Tasks;
using System.Web.OData;
using Foundation.Api.ApiControllers;
using Foundation.Test.Model.DomainModels;

namespace Foundation.Api.ApiControllers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ParameterAttribute : Attribute
    {
        public string Name { get; private set; }

        public Type Type { get; private set; }

        public bool IsOptional { get; private set; }

        public ParameterAttribute(string name, Type type, bool isOptional = false)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            Name = name;
            Type = type;
            IsOptional = isOptional;
        }
    }
}

namespace Foundation.Test.Api.ApiControllers
{
    public class TestController : DtoController<TestModel>
    {
        [Action]
        [Parameter(""Parameter1"", typeof(string))]
        [ParameterAttribute(""Parameter1"", typeof(string))]
        [Foundation.Api.ApiControllers.Parameter(""Parameter1"", typeof(string))]
        [Foundation.Api.ApiControllers.ParameterAttribute(""Parameter1"", typeof(string))]
        public virtual async Task Do(ODataActionParameters actionParameters)
        {

        }
    }
}
";
            DiagnosticResult nonCamelCaseParameterName1 = new DiagnosticResult
            {
                Id = nameof(CamelCaseParameterNameAnalyzer),
                Message = FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerMessage,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 39, 10) }
            };

            DiagnosticResult nonCamelCaseParameterName2 = new DiagnosticResult
            {
                Id = nameof(CamelCaseParameterNameAnalyzer),
                Message = FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerMessage,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 40, 10) }
            };

            DiagnosticResult nonCamelCaseParameterName3 = new DiagnosticResult
            {
                Id = nameof(CamelCaseParameterNameAnalyzer),
                Message = FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerMessage,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 41, 10) }
            };

            DiagnosticResult nonCamelCaseParameterName4 = new DiagnosticResult
            {
                Id = nameof(CamelCaseParameterNameAnalyzer),
                Message = FoundationAnalyzersResources.CamelCaseParameterNameAnalyzerMessage,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 42, 10) }
            };

            VerifyCSharpDiagnostic(sourceCodeOfDtoControllerWithActionAndParameter, nonCamelCaseParameterName1, nonCamelCaseParameterName2, nonCamelCaseParameterName3, nonCamelCaseParameterName4);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CamelCaseParameterNameAnalyzer();
        }
    }
}
