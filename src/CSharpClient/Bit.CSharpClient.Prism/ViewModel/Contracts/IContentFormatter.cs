using System.Diagnostics.CodeAnalysis;

namespace Bit.ViewModel.Contracts
{
    public interface IContentFormatter
    {
        string Serialize<T>([MaybeNull]T obj);

        T Deserialize<T>(string objAsStr);
    }
}
