using Bit.Tooling.CodeAnalyzer.BitAnalyzers.ViewModel;
using Bit.Tooling.CodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeAnalyzer.Test.BitAnalyzers.ViewModel
{
    [TestClass]
    public class UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectly()
        {
            DiagnosticResult appCenterUsage1 = new DiagnosticResult
            {
                Id = nameof(UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer),
                Message = UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 27, 13) }
            };

            DiagnosticResult appCenterUsage2 = new DiagnosticResult
            {
                Id = nameof(UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer),
                Message = UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 28, 13) }
            };

            DiagnosticResult appCenterUsage3 = new DiagnosticResult
            {
                Id = nameof(UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer),
                Message = UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 29, 13) }
            };

            await VerifyCSharpDiagnostic(appCenterUsage1, appCenterUsage2, appCenterUsage3);
        }

        private readonly string basePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\..\BitAnalyzers\ViewModel\BitPrismTestsProj")).FullName;

        public override async Task<Project> CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            return (await GetWorkspace(basePath, "BitPrismTestsProj.sln")).CurrentSolution.Projects.Single();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseBitTelemetryServicesInsteadOfUsingAppCenterCrashesAndAnalyticsDirectlyAnalyzer();
        }
    }
}
