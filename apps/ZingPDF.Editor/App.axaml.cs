using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ZingPDF.Editor;

public sealed partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow(desktop.Args?.Contains("--demo") == true ? "--demo" : desktop.Args?.FirstOrDefault(a => !a.StartsWith('-')));
        base.OnFrameworkInitializationCompleted();
    }
}
