using BitCodeAnalyzer.BitAnalyzers.Data.EntityFramework;
using BitCodeAnalyzer.SystemAnalyzers;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace BitCodeAnalyzer.Test.BitAnalyzers.Data.EntityFramework
{
    [TestClass]
    public class AsNoTrackingAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public void AsNoTrackingMustBeCalledBeforeAllOtherMethodCallsOfDbQueryAndDbSqlQuery()
        {
            DiagnosticResult notCalledAsNoTracking1 = new DiagnosticResult
            {
                Id = nameof(AsNoTrackingAnalyzer),
                Message = AsNoTrackingAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"EntityFrameworkFullAsNoTrackingCallTests\Program.cs"), 13, 33) }
            };

            DiagnosticResult notCalledAsNoTracking2 = new DiagnosticResult
            {
                Id = nameof(AsNoTrackingAnalyzer),
                Message = AsNoTrackingAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"EntityFrameworkFullAsNoTrackingCallTests\Program.cs"), 15, 33) }
            };

            DiagnosticResult notCalledAsNoTracking3 = new DiagnosticResult
            {
                Id = nameof(AsNoTrackingAnalyzer),
                Message = AsNoTrackingAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"EntityFrameworkFullAsNoTrackingCallTests\Program.cs"), 17, 42) }
            };

            VerifyCSharpDiagnostic(notCalledAsNoTracking1, notCalledAsNoTracking2, notCalledAsNoTracking3);
        }

        private readonly string basePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\BitAnalyzers\Data\EntityFramework\EntityFrameworkFullAsNoTrackingCallTests")).FullName;

        public override Project CreateProject(string[] sources, string language = "C#")
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            workspace.OpenSolutionAsync(Path.Combine(basePath, "EntityFrameworkFullAsNoTrackingCallTests.sln"), CancellationToken.None).Wait();

            return workspace.CurrentSolution.Projects.Single();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new AsNoTrackingAnalyzer();
        }
    }
}
