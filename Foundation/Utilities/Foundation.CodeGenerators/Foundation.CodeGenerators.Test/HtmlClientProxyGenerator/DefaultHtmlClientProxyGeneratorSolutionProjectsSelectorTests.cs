using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProjectInfo = Foundation.CodeGenerators.Model.ProjectInfo;

namespace Foundation.CodeGenerators.Test.HtmlClientProxyGenerator
{
    [TestClass]
    public class DefaultHtmlClientProxyGeneratorSolutionProjectsSelectorTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("HTMLClientProxyGenerator")]
        public async Task DefaultHtmlClientProxyGeneratorSolutionProjectsSelectorShouldReturnProjectsAsDesired()
        {
            using (Workspace workspace = GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<Project> projects = solution.Projects.ToList();

                IList<Project> orderedProjects = (new DefaultHtmlClientProxyGeneratorSolutionProjectsSelector().GetInvolveableProjects(workspace, solution, projects,
                        new HtmlClientProxyGeneratorMapping { SourceProjects = new[] { new ProjectInfo { Name = "Foundation.Model" }, new ProjectInfo { Name = "Foundation.Api" } } }));

                Assert.AreEqual(2, orderedProjects.Count);

                Assert.IsTrue(
                    orderedProjects.Select(p => p.Name).SequenceEqual(new[] { "Foundation.Model", "Foundation.Api" }));
            }
        }
    }
}
