namespace Bit.Model.Contracts
{
    public interface IArchivableEntity : IEntity
    {
        bool IsArchived { get; set; }
    }
}
