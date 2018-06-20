using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IBitConfigProvider
    {
        string GetSolutionFilePath(Workspace workspace);

        BitConfig GetConfiguration(Workspace workspace);

        string Version { get; set; }
    }
}
