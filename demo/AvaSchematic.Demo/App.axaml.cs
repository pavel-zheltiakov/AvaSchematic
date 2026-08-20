using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AvaSchematic.Demo;

/// <summary>
/// The demo, on whichever kind of host it finds itself.
///
/// A desktop lifetime wants a window to put the view in; a browser tab has no windows at all and hands
/// over a single view instead. Both get the same <see cref="MainView"/>, which is why the demo on the
/// site is the demo on your machine rather than a cut-down imitation of it.
/// </summary>
public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new Window
                {
                    Title = "AvaSchematic — schematic and diagram control for Avalonia",
                    Width = 1500,
                    Height = 960,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Content = new MainView()
                };
                break;

            case ISingleViewApplicationLifetime single:
                single.MainView = new MainView();
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
