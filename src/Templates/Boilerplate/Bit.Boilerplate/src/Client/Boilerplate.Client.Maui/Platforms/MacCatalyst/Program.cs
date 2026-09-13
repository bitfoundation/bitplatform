// [mirror] apple entry point - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/iOS/Program.cs

using UIKit;

namespace Boilerplate.Client.Maui.Platforms.MacCatalyst;

public partial class Program
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
