using Bit.Tooling.Core.Model;

namespace Bit.Tooling.Core.Contracts
{
    public interface IBitConfigProvider
    {
        string GetBitConfigFilePath();

        BitConfig GetConfiguration();
    }
}
