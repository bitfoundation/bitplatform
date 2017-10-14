using BitChangeSetManager.Model;

namespace BitChangeSetManager.DataAccess.Contracts
{
    public interface IChangeSetsRepository : IBitChangeSetManagerRepository<ChangeSet>
    {
    }
}