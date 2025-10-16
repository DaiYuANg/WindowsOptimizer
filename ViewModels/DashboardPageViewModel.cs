using Microsoft.Extensions.Logging;
using Serilog;
using WindowsOptimizer.Service;

namespace WindowsOptimizer.ViewModels;

public class DashboardPageViewModel : BindableBase
{
    private readonly ISystemInfoService _systemInfoService;

    public DashboardPageViewModel(
        ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
        Log.Information("DashboardPageViewModel created!");
        // 初始化属性
        OSVersion = _systemInfoService.OSVersion;
        MachineName = _systemInfoService.MachineName;
        CPUInfo = _systemInfoService.CPUInfo;
        TotalMemory = _systemInfoService.TotalMemory;
        FreeMemory = _systemInfoService.FreeMemory;
        Log.Information(OSVersion);
        NavigateSettingsCommand = new DelegateCommand(() =>
        {
            // 使用 RegionManager 导航
        });
        Log.Information("Dashboard 初始化完成");
    }

    public string OSVersion { get; }
    public string MachineName { get; }
    public string CPUInfo { get; }
    public string TotalMemory { get; }
    public string FreeMemory { get; }

    public DelegateCommand NavigateSettingsCommand { get; }
}