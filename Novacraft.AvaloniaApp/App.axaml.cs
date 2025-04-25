using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Novacraft.AvaloniaApp.Views;
using Classic.Avalonia.Theme;

namespace Novacraft.AvaloniaApp;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            Application.Current!.Styles.Insert(0, new ClassicTheme());
        }

        base.OnFrameworkInitializationCompleted();
    }
}