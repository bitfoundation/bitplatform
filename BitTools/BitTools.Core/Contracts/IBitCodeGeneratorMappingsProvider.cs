using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using BitTools.Core.Model;

namespace BitTools.Core.Contracts
{
    public interface IBitCodeGeneratorMappingsProvider
    {
        IList<BitCodeGeneratorMapping> GetBitCodeGeneratorMappings(Solution solution, IList<Project> projects);
    }
}
