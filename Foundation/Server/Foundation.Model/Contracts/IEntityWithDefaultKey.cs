namespace Foundation.Model.Contracts
{
    public interface IEntityWithDefaultKey<TKey> : IEntity
        where TKey : struct
    {
        TKey Id { get; set; }
    }
}
