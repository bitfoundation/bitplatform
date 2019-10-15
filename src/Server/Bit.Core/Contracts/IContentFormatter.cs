namespace Bit.Core.Contracts
{
    /// <summary>
    /// It serialize/deSerialize objects to/from json/xml etc based on what implementation is provided.
    /// It is used mostly in <see cref="ILogStore"/> implementations.
    /// </summary>
    public interface IContentFormatter
    {
        string Serialize<T>(T obj);

        T Deserialize<T>(string objAsStr);
    }
}