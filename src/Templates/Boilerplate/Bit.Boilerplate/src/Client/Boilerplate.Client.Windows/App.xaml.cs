using System.IO.IsolatedStorage;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Collections;
using Microsoft.Win32;
using System.Windows.Media;
using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Windows;

public partial class App
{
    public App()
    {
        InitializeComponent();

        var splash = new SplashScreen(typeof(App).Assembly, @"Resources\SplashScreen.png");
        splash.Show(autoClose: true, topMost: true);

        Resources["PrimaryBgColor"] = new BrushConverter().ConvertFrom(IsDarkTheme() ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor);
    }

    private static bool IsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var value = key?.GetValue("AppsUseLightTheme");
        return value is int i && i == 0;
    }

    const string WindowsStorageFilename = "windows.storage.json";

    private void App_Startup(object sender, StartupEventArgs e)
    {
        // Restore application-scope property from isolated storage
        using IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
        try
        {
            using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.Open, storage);
            foreach (DictionaryEntry item in JsonSerializer.Deserialize<IDictionary>(stream)!)
            {
                Properties.Add(item.Key, item.Value);
            }
        }
        catch (IsolatedStorageException exp) when (exp.InnerException is FileNotFoundException) { }
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        // Persist application-scope property to isolated storage
        IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForDomain();
        using IsolatedStorageFileStream stream = new IsolatedStorageFileStream(WindowsStorageFilename, FileMode.Create, storage);
        using StreamWriter writer = new StreamWriter(stream);
        writer.Write(JsonSerializer.Serialize(Properties));
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            ((MainWindow)MainWindow).AppWebView.Services.GetRequiredService<IExceptionHandler>().Handle(e.Exception);
        }
        catch
        {
            var errorMessage = e.Exception.ToString();
            System.Windows.Clipboard.SetText(errorMessage);
            System.Windows.Forms.MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        e.Handled = true;
    }
}

