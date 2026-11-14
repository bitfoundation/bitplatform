namespace Bit.Blow.Tests;

/// <summary>
/// Locks in the fix for #14914: an interface method with more than eight parameters needs a bridge built for its exact
/// signature when an app compiled with profile-guided AOT reaches it on the interpreter, and none exists - the build and
/// every other test stay green while the published app fails at runtime. Wide calls take a parameter object instead
/// (see <see cref="BlowResizeOptions"/>).
/// </summary>
[TestClass]
public class InterfaceParameterLimitTests
{
    private const int MaxParameters = 8;

    [TestMethod]
    public void NoInterfaceMethodInTheLibraryTakesMoreThanEightParameters()
    {
        var tooWide = typeof(IBlowContext).Assembly.GetTypes()
            .Where(t => t.IsInterface)
            .SelectMany(t => t.GetMethods().Select(m => (Type: t, Method: m)))
            .Where(x => x.Method.GetParameters().Length > MaxParameters)
            .Select(x => $"{x.Type.FullName}.{x.Method.Name} ({x.Method.GetParameters().Length} parameters)")
            .ToArray();

        Assert.AreEqual(0, tooWide.Length, string.Join(Environment.NewLine, tooWide));
    }
}
