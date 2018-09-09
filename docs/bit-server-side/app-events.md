# Summary
There are situations you want to do something during the life-cycle of a web application.
For example you want to initialize your cache **when the applicaiton starts**. 
Or you want to run some code **when the applications finishes**.

In these situations you can create a class implementing `IAppEvents` interface to describe your logic. Then registering it using `DependencyManager`.

# Example

``` c#
public class CacheAppEvents : IAppEvents
{
    // You can inject whatever you want using property injection.
    
    public void OnAppStartup()
    {
        // Initializing your cache.
        // This code will be executed once, when application starts.
    }

    public void OnAppEnd()
    {
      // Whatever you want to do when application stops.      
    }
}
```

And finally you should register it as one of your dependencies:
``` c#
// Put it just besides other dependency registerations in your code.
dependencyManager.RegisterAppEvents<CacheAppEvents>();
```
