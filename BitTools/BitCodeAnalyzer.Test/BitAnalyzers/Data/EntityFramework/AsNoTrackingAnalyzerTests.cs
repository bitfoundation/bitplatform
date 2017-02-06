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
            DiagnosticResult notCalledAsNoTracking = new DiagnosticResult
            {
                Id = nameof(ClassWithoutModifierAnalyzer),
                Message = ClassWithoutModifierAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"EntityFrameworkFullAsNoTrackingCallTests\Program.cs"), 25, 12) }
            };

            VerifyCSharpDiagnostic(notCalledAsNoTracking);
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
