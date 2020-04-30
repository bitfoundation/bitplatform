using Bit.Tooling.CodeAnalyzer.BitAnalyzers.ViewModel;
using Bit.Tooling.CodeAnalyzer.Test.Helpers;
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

namespace Bit.Tooling.CodeAnalyzer.Test.BitAnalyzers.ViewModel
{
    [TestClass]
    public class UseRegisterForNavInsteadOfRegisterForNavigationAnalyzerTests : DiagnosticVerifier
    {
        [TestMethod]
        [TestCategory("Analyzer")]
        public async Task UseRegisterForNavInsteadOfRegisterForNavigationTest()
        {
            DiagnosticResult registerForNavigationUsage1 = new DiagnosticResult
            {
                Id = nameof(UseRegisterForNavInsteadOfRegisterForNavigationAnalyzer),
                Message = UseRegisterForNavInsteadOfRegisterForNavigationAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation(Path.Combine(basePath, @"Tests.cs"), 21, 13) }
            };

            await VerifyCSharpDiagnostic(registerForNavigationUsage1);
        }

        private readonly string basePath = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\..\BitAnalyzers\ViewModel\BitPrismTestsProj")).FullName;

        public override async Task<Project> CreateProject(string[] sources, string language = LanguageNames.CSharp)
        {
            return (await GetWorkspace(basePath, "BitPrismTestsProj.sln")).CurrentSolution.Projects.Single();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UseRegisterForNavInsteadOfRegisterForNavigationAnalyzer();
        }
    }
}
