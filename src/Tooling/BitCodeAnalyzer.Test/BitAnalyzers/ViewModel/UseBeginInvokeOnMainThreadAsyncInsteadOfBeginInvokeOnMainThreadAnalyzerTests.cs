using BitCodeAnalyzer.BitAnalyzers.ViewModel;
using BitCodeAnalyzer.Test.Helpers;
using Microsoft.Build.Locator;
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
    public class UseBeginInvokeOnMainThreadAsyncInsteadOfBeginInvokeOnMainThreadAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task UseBeginInvokeOnMainThreadAsyncInsteadOfBeginInvokeOnMainThread()
        {
            DiagnosticResult deviceServiceUsage = new DiagnosticResult
            {
                Id = nameof(UseBeginInvokeOnMainThreadAsyncInsteadOfBeginInvokeOnMainThreadAnalyzer),
                Message = UseBeginInvokeOnMainThreadAsyncInsteadOfBeginInvokeOnMainThreadAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 25, 13) }
            };

            await VerifyCSharpDiagnostic(deviceServiceUsage);
        }

        private readonly string basePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\..\BitAnalyzers\ViewModel\BitPrismTestsProj")).FullName;

        public override async Task<Project> CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string, string>()
            {
                { "TargetFramework", "netstandard2.0" }
            });

            await workspace.OpenSolutionAsync(Path.Combine(basePath, "BitPrismTestsProj.sln"));

            return workspace.CurrentSolution.Projects.Single();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseBeginInvokeOnMainThreadAsyncInsteadOfBeginInvokeOnMainThreadAnalyzer();
        }
    }
}
