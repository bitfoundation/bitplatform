using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IBitConfigProvider
    {
        BitConfig GetConfiguration(Workspace workspace, Solution solution, IList<Project> projects);

        string Version { get; set; }
    }
}
