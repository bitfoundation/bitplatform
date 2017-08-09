using System.Collections.Generic;
using BitTools.Core.Model;
using Microsoft.CodeAnalysis;

namespace BitTools.Core.Contracts
{
    public interface IBitConfigProvider
    {
        BitConfig GetConfiguration(string solutionFilePath);

        string Version { get; set; }
    }
}
