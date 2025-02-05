namespace Bit.Besql;

internal class NoopBesqlStorage : IBesqlStorage
{
    public Task Init(string filename)
    {
        return Task.CompletedTask;
    }

    public Task Persist(string filename)
    {
        return Task.CompletedTask;
    }

    public void PauseSync()
    {
    }

    public Task ResumeSync()
    {
        return Task.CompletedTask;
    }
}
