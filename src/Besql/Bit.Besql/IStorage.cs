namespace Bit.Besql;

public interface IStorage
{
    Task<int> SyncDb(string filename);
}
