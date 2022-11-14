using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Test.Helpers
{
    public class CodeGeneratorTest
    {
        public virtual Project CreateProjectFromSourceCodesWithExistingSolution(Solution existingSolution, params string[] sourceCodes)
        {
            ProjectId projectId = ProjectId.CreateNewId(debugName: "TestProjectName");

            existingSolution = existingSolution.AddProject(projectId, "TestProjectName", "TestProjectName", LanguageNames.CSharp);

            AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                .ToList()
                .ForEach(asm =>
                {
                    existingSolution = existingSolution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(asm.Location));
                });

            for (int i = 0; i < sourceCodes.Length; i++)
            {
                DocumentId fileDocId = DocumentId.CreateNewId(projectId, debugName: $"File{i}.cs");

                existingSolution = existingSolution.AddDocument(fileDocId, $"File{i}.cs", SourceText.From(sourceCodes[i]));
            }

            return existingSolution.GetProject(projectId);
        }

        public virtual Project CreateProjectFromSourceCodes(params string[] sourceCodes)
        {
            Solution solution = new AdhocWorkspace()
                .CurrentSolution;

            return CreateProjectFromSourceCodesWithExistingSolution(solution, sourceCodes);
        }

        public virtual async Task<Workspace> GetWorkspace()
        {
            string solutionPath = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\Bit.sln");

            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create(new Dictionary<string, string>()
            {
                { "TargetFramework", "net7.0" }
            });

            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            await workspace.OpenSolutionAsync(solutionPath);

            return workspace;
        }

        private void Workspace_WorkspaceFailed(object sender, WorkspaceDiagnosticEventArgs e)
        {
            if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                Debug.WriteLine(e.Diagnostic.Message);
        }
    }
}
