using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.IO;

namespace Foundation.CodeGenerators.Test
{
    public class CodeGeneratorTest
    {
        public virtual Project CreateProjectFromSourceCodes(params string[] sourceCodes)
        {
            ProjectId projectId = ProjectId.CreateNewId(debugName: "TestProjectName");

            Solution solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProjectName", "TestProjectName", LanguageNames.CSharp);

            AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic)
                .ToList()
                .ForEach(asm =>
                {
                    solution = solution.AddMetadataReference(projectId, MetadataReference.CreateFromFile(asm.Location));
                });

            for (int i = 0; i < sourceCodes.Length; i++)
            {
                DocumentId fileDocId = DocumentId.CreateNewId(projectId, debugName: $"File{i}.cs");

                solution = solution.AddDocument(fileDocId, $"File{i}.cs", SourceText.From(sourceCodes[i]));
            }

            return solution.GetProject(projectId);
        }

        public virtual Workspace GetWorkspace()
        {
            string solutionPath = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\..\Foundation.sln");

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            workspace.OpenSolutionAsync(solutionPath, CancellationToken.None).Wait();

            return workspace;
        }
    }
}
