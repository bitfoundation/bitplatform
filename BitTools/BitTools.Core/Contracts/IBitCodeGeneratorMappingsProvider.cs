using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using BitTools.Core.Model;

namespace BitTools.Core.Contracts
{
    public interface IBitCodeGeneratorMappingsProvider
    {
        IList<BitCodeGeneratorMapping> GetBitCodeGeneratorMappings(Workspace workspace, string solutionFilePath, IList<string> projectNames);
    }
}
