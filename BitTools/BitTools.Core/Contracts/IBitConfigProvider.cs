using BitTools.Core.Model;

namespace BitTools.Core.Contracts
{
    public interface IBitConfigProvider
    {
        BitConfig GetConfiguration(string solutionFilePath);

        string Version { get; set; }
    }
}
