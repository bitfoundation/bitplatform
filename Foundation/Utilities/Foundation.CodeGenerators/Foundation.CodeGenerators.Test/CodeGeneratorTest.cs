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
        public virtual Project CreateProjectFromSourceCodesWithExistingSolution(Solution existingSolution, params string[] sourceCodes)
        {
            ProjectId projectId = ProjectId.CreateNewId(debugName: "TestProjectName");

            existingSolution = existingSolution.AddProject(projectId, "TestProjectName", "TestProjectName", LanguageNames.CSharp);

            AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic)
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

        public virtual Workspace GetWorkspace()
        {
            string solutionPath = Path.Combine(Environment.CurrentDirectory, @"..\..\..\..\..\..\Foundation.sln");

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            workspace.OpenSolutionAsync(solutionPath, CancellationToken.None).Wait();

            return workspace;
        }
    }
}
