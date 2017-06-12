using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IBitCodeGeneratorOrderedProjectsProvider
    {
        IList<Project> GetInvolveableProjects(Solution solution, IList<Project> projects,
            BitCodeGeneratorMapping htmlClientProxyGeneratorMapping);
    }
}
