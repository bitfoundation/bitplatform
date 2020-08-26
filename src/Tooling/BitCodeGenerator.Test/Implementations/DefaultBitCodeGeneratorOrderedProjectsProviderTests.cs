using Bit.Tooling.Core.Model;
using Bit.Tooling.CodeGenerator.Implementations;
using Bit.Tooling.CodeGenerator.Test.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bit.Tooling.CodeGenerator.Test.Implementations
{
    [TestClass]
    public class DefaultBitCodeGeneratorOrderedProjectsProviderTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("BitCodeGenerator")]
        public async Task DefaultBitCodeGeneratorOrderedProjectsProviderTestsShouldReturnProjectsAsDesired()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<Project> projects = solution.Projects.ToList();

                IList<Project> orderedProjects = (new DefaultBitCodeGeneratorOrderedProjectsProvider().GetInvolveableProjects(workspace, projects,
                        new BitCodeGeneratorMapping { SourceProjects = new[] { new Core.Model.ProjectInfo { Name = "Bit.Universal.Model" }, new Core.Model.ProjectInfo { Name = "Bit.Server.OData" } } }));

                Assert.AreEqual(2, orderedProjects.Count);

                Assert.IsTrue(
                    orderedProjects.Select(p => p.Name).SequenceEqual(new[] { "Bit.Universal.Model", "Bit.Server.OData" }));
            }
        }
    }
}
