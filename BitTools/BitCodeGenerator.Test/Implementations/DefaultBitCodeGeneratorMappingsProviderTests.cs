using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitCodeGenerator.Test.Helpers;
using BitTools.Core.Model;
using BitCodeGenerator.Implementations;

namespace BitCodeGenerator.Test.Implementations
{
    [TestClass]
    public class DefaultBitCodeGeneratorMappingsProviderTests : CodeGeneratorTest
    {
        [TestMethod, TestCategory("BitCodeGenerator")]
        public virtual async Task DefaultBitCodeGeneratorMappingsProviderShouldReturnDesiredMappings()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<string> projectNames = solution.Projects.Select(p => p.Name).ToList();

                IList<BitCodeGeneratorMapping> bitCodeGeneratorMappings = new DefaultBitCodeGeneratorMappingsProvider(
                    new DefaultBitConfigProvider())
                    .GetBitCodeGeneratorMappings(workspace, workspace.CurrentSolution.FilePath, projectNames);

                Assert.AreEqual(2, bitCodeGeneratorMappings.Count);

                Assert.AreEqual("Bit", bitCodeGeneratorMappings.First().EdmName);
                Assert.AreEqual("Test", bitCodeGeneratorMappings.Last().EdmName);
            }
        }

        [TestMethod, TestCategory("BitCodeGenerator")]
        public virtual async Task DefaultBitCodeGeneratorMappingsProviderShouldReturnNoMappings()
        {
            using (Workspace workspace = await GetWorkspace())
            {
                Solution solution = workspace.CurrentSolution;

                List<string> projectNames = solution.Projects.Where(p => p.Name == "Bit.Core").Select(p => p.Name).ToList();

                IList<BitCodeGeneratorMapping> bitCodeGeneratorMappings = new DefaultBitCodeGeneratorMappingsProvider(
                    new DefaultBitConfigProvider())
                    .GetBitCodeGeneratorMappings(workspace, workspace.CurrentSolution.FilePath, projectNames);

                Assert.AreEqual(0, bitCodeGeneratorMappings.Count);
            }
        }
    }
}
