// [mirror] apple entry point - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/Platforms/MacCatalyst/Program.cs

using UIKit;

namespace Boilerplate.Client.Maui.Platforms.iOS;

public partial class Program
{
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
