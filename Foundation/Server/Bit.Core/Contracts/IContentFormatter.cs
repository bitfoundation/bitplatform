namespace Foundation.Core.Contracts
{
    public interface IContentFormatter
    {
        string Serialize<T>(T obj);

        T DeSerialize<T>(string objAsStr);
    }
}