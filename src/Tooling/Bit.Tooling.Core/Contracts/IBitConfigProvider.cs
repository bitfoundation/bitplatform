using BitTools.Core.Model;

namespace BitTools.Core.Contracts
{
    public interface IBitConfigProvider
    {
        string GetBitConfigFilePath();

        BitConfig GetConfiguration();
    }
}
