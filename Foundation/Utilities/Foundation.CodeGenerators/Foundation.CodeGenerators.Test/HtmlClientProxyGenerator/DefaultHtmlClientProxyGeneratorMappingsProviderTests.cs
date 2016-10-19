using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation.CodeGenerators.Implementations;
using Foundation.CodeGenerators.Implementations.HtmlClientProxyGenerator;
using Foundation.CodeGenerators.Model;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Foundation.CodeGenerators.Test.HtmlClientProxyGenerator
{
    [TestClass]
    public class DefaultHtmlClientProxyGeneratorMappingsProviderTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("HTMLClientProxyGenerator")]
        public virtual async Task DefaultHtmlClientProxyGeneratorMappingsProviderShouldReturnDesiredMappings()
        {
            using (Workspace workspace = GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<Project> projects = solution.Projects.ToList();

                IList<HtmlClientProxyGeneratorMapping> contextMappings = new DefaultHtmlClientProxyGeneratorMappingsProvider(
                    new DefaultFoundationVSPackageConfigurationProvider())
                    .GetHtmlClientProxyGeneratorMappings(workspace, solution, projects);

                Assert.AreEqual(2, contextMappings.Count);

                Assert.AreEqual("Foundation", contextMappings.First().EdmName);
                Assert.AreEqual("Test", contextMappings.Last().EdmName);
            }
        }

        [TestMethod, TestCategory("HTMLClientProxyGenerator")]
        public virtual async Task DefaultHtmlClientProxyGeneratorMappingsProviderShouldReturnNoMappings()
        {
            using (Workspace workspace = GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<Project> projects = solution.Projects.Where(p => p.Name == "Foundation.Core").ToList();

                IList<HtmlClientProxyGeneratorMapping> contextMappings = new DefaultHtmlClientProxyGeneratorMappingsProvider(
                    new DefaultFoundationVSPackageConfigurationProvider())
                    .GetHtmlClientProxyGeneratorMappings(workspace, solution, projects);

                Assert.AreEqual(0, contextMappings.Count);
            }
        }
    }
}
