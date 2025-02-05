namespace Bit.Besql;

public interface IBesqlStorage
{
    Task Init(string filename);

    Task Persist(string filename);

    void PauseSync();

    Task ResumeSync();
}
