namespace Bit.Model.Contracts
{
    public interface IWithDefaultKey<TKey>
    {
        TKey Id { get; set; }
    }
}
