namespace Foundation.Model.Contracts
{
    public interface IsArchivableEntity : IEntity
    {
        bool IsArchived { get; set; }
    }
}
