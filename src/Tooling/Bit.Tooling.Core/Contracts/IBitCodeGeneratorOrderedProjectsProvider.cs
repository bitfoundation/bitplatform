using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Contracts
{
    public interface IBitCodeGeneratorOrderedProjectsProvider
    {
        IList<Project> GetInvolveableProjects(Workspace workspace, IList<Project> projects,
            BitCodeGeneratorMapping bitCodeGeneratorMapping);
    }
}
