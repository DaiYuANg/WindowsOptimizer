using System.IO;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Serilog;
using WindowsControlPanel.Context;
using WindowsControlPanel.Service;
using WindowsControlPanel.Views;

namespace WindowsControlPanel;

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
        containerRegistry.RegisterForNavigation<OptimizeOptionPage>("OptimizeOption");
        containerRegistry.RegisterForNavigation<SecurityVirtualizationPage>("SecurityVirtualization");
        containerRegistry.RegisterForNavigation<FeaturePlaceholderPage>("FeaturePlaceholder");

        containerRegistry.RegisterSingleton<ISystemInfoService, SystemInfoService>();

        containerRegistry.RegisterSingleton<AppDbContext>(() =>
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowsOptimizer", "appdata.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureCreated(); // 自动创建数据库和表
            return db;
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug() // 最低日志级别
            .WriteTo.Console() // 控制台输出
            .WriteTo.File("logs\\app.log", rollingInterval: RollingInterval.Day,
                encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            ) // 文件输出
            .CreateLogger();

        containerRegistry.RegisterInstance(Log.Logger);
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
