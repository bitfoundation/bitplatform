using BitCodeAnalyzer.BitAnalyzers.ViewModel;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitCodeAnalyzer.Test.BitAnalyzers.ViewModel
{
    [TestClass]
    public class UseSubscribeAsyncInsteadOfSubscribeAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task UseSubscribeAsyncInsteadOfSubscribeAnalyzerTest()
        {
            DiagnosticResult subscribeUsage1 = new DiagnosticResult
            {
                Id = nameof(UseSubscribeAsyncInsteadOfSubscribeAnalyzer),
                Message = UseSubscribeAsyncInsteadOfSubscribeAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 11, 13) }
            };

            DiagnosticResult subscribeUsage2 = new DiagnosticResult
            {
                Id = nameof(UseSubscribeAsyncInsteadOfSubscribeAnalyzer),
                Message = UseSubscribeAsyncInsteadOfSubscribeAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 13, 13) }
            };

            await VerifyCSharpDiagnostic(subscribeUsage1, subscribeUsage2);
        }

        private readonly string basePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\..\BitAnalyzers\ViewModel\SubscribeAsyncTestProj")).FullName;

        public override async Task<Project> CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string, string>()
            {
                { "TargetFramework", "net461" }
            });

            await workspace.OpenSolutionAsync(Path.Combine(basePath, "SubscribeAsyncTestProj.sln"));

            return workspace.CurrentSolution.Projects.Single();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseSubscribeAsyncInsteadOfSubscribeAnalyzer();
        }
    }
}
