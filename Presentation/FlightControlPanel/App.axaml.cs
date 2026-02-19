using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;

namespace FlightControlPanel;

public partial class App : Application
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
        }

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        MainWindowViewModel.CoordGeneratorUri = configuration["CoordGeneratorUri"]!;
        MainWindowViewModel.FlightControllerRestUri = configuration["FlightControllerRestUri"]!;
        MainWindowViewModel.FlightControllerGrpcUri = configuration["FlightControllerGrpcUri"]!;

        base.OnFrameworkInitializationCompleted();
    }

}