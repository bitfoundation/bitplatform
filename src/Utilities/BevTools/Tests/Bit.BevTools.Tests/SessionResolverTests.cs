using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

[TestClass]
public class SessionResolverTests
{
    [TestMethod]
    public void Synchronization_context_mapping_does_not_keep_a_disposed_session_alive()
    {
        var (sessionReference, context) = CreateMappedSession();

        for (var i = 0; i < 5 && sessionReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.IsFalse(sessionReference.IsAlive);
        GC.KeepAlive(context);
    }

    private static (WeakReference Session, SynchronizationContext Context) CreateMappedSession()
    {
        var services = new ServiceCollection();
        services.AddBitBevToolsServices(options => options.Enabled = true);
        using var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<BevToolsSession>();
        session.EnsureActivated();
        var context = new SynchronizationContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            SessionResolver.NoteCurrentContext(session);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        var reference = new WeakReference(session);
        scope.Dispose();
        return (reference, context);
    }
}
