namespace Bit.Besql;

/// <summary>
/// <inheritdoc cref="IBitBesqlStorage"/>
/// </summary>
internal class BitBesqlNoopStoage : IBitBesqlStorage
{
    public Task Persist(string filename)
    {
        return Task.CompletedTask;
    }

    public Task Load(string filename)
    {
        return Task.CompletedTask;
    }

    public void PauseAutomaticPersistent()
    {
    }

    public Task ResumeAutomaticPersistent()
    {
        return Task.CompletedTask;
    }
}
