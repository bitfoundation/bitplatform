namespace Bit.Besql;

public interface IBesqlStorage
{
    Task<int> SyncDb(string filename);
}
