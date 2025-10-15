using System.Windows;
using WindowsOptimizer.Service;
using WindowsOptimizer.Views;
using Wpf.Ui.Appearance;

namespace WindowsOptimizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RequestNavigate("ContentRegion", "Dashboard");
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<DashboardPage>("Dashboard");

        containerRegistry.RegisterSingleton<ISystemInfoService, SystemInfoService>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // moduleCatalog.AddModule<CoreModule>();
        // moduleCatalog.AddModule<HotkeyModule>();
        // moduleCatalog.AddModule<WslModule>();
    }

    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }
}