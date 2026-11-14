namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// What a link of the app's own pages means in a view. A preview keeps the view on its page when a link names an address
/// of the app (See PreviewPage), and a page that renders the app's components, such as the settings page's host, tells
/// it here which of those addresses another Workbench page stands for, so the link opens that one instead.
/// </summary>
public sealed class WorkbenchAppLinks
{
    private readonly List<Func<Uri, Func<Task>?>> followers = [];

    /// <summary>Adds what a page makes of the app's links, for as long as the returned handle isn't disposed.</summary>
    public IDisposable Add(Func<Uri, Func<Task>?> follower)
    {
        followers.Add(follower);
        return new Registration(() => followers.Remove(follower));
    }

    /// <summary>What to do for a link of the app to this address; null when no page here stands for it.</summary>
    public Func<Task>? Follow(Uri target) => followers.Select(follower => follower(target)).FirstOrDefault(action => action is not null);

    private sealed class Registration(Action remove) : IDisposable
    {
        public void Dispose() => remove();
    }
}
