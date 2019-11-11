namespace Bit.ViewModel.Contracts
{
    public interface IContentFormatter
    {
        string Serialize<T>(T obj);

        T Deserialize<T>(string objAsStr);
    }
}
