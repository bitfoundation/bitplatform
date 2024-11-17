//+:cnd:noEmit
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Collections;
using System.Windows.Media;
using System.IO.IsolatedStorage;
using Boilerplate.Client.Core.Styles;

namespace Boilerplate.Client.Windows;

public partial class App
{
    public App()
    {
        InitializeComponent();

        var splash = new SplashScreen(typeof(App).Assembly, @"Resources\SplashScreen.png");
        splash.Show(autoClose: true, topMost: true);

        ConfigureAppTheme();
    }

    private void ConfigureAppTheme()
    {
        //#if (framework == 'net9.0')
        ThemeMode = ThemeMode.System;
        Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml", UriKind.Absolute)
        });
        //#endif

        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var value = key?.GetValue("AppsUseLightTheme");
        var isDark = value is int i && i == 0;

        Resources["PrimaryBgColor"] = new BrushConverter().ConvertFrom(isDark ? ThemeColors.PrimaryDarkBgColor : ThemeColors.PrimaryLightBgColor);
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

