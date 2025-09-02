using System.Configuration;
using System.Data;
using System.Management;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using WindowsOptimizer.Service;

namespace WindowsOptimizer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public new static App Current => (App)Application.Current;

    private IServiceProvider? ServiceProvider { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();

        // 解析主窗口
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 注册服务
        services.AddSingleton<ISystemInfoService, SystemInfoService>();

        // 注册窗口
        services.AddTransient<MainWindow>();
        // bool vbsEnabled = IsVbsEnabled();
    }
}