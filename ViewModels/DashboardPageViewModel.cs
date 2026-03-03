using Prism.Commands;
using Prism.Mvvm;
using WindowsControlPanel.Service;

namespace WindowsControlPanel.ViewModels;

public class DashboardPageViewModel : BindableBase
{
    private readonly ISystemControlService _systemControlService;
    private readonly IRegionManager _regionManager;
    private string _systemInfoSummary = "Loading system information...";
    private string _lastRefreshTime = string.Empty;
    private string _securitySummary = string.Empty;
    private string _virtualizationSummary = string.Empty;

    public DashboardPageViewModel(
        ISystemControlService systemControlService,
        IRegionManager regionManager)
    {
        _systemControlService = systemControlService;
        _regionManager = regionManager;

        OpenSecurityCommand = new DelegateCommand(() => NavigateToHub("security"));
        OpenDevelopmentCommand = new DelegateCommand(() => NavigateToHub("development"));
        OpenGameCommand = new DelegateCommand(() => NavigateToHub("gaming"));
        OpenStartupCommand = new DelegateCommand(() => NavigateToHub("startup"));
        OpenCleanupCommand = new DelegateCommand(() => NavigateToHub("cleanup"));
        OpenNetworkCommand = new DelegateCommand(() => NavigateToHub("network"));
        RefreshCommand = new DelegateCommand(async () => await RefreshDataAsync());

        _ = RefreshDataAsync();
    }

    public string SystemInfoSummary
    {
        get => _systemInfoSummary;
        private set => SetProperty(ref _systemInfoSummary, value);
    }

    public string LastRefreshTime
    {
        get => _lastRefreshTime;
        private set => SetProperty(ref _lastRefreshTime, value);
    }

    public string SecuritySummary
    {
        get => _securitySummary;
        private set => SetProperty(ref _securitySummary, value);
    }

    public string VirtualizationSummary
    {
        get => _virtualizationSummary;
        private set => SetProperty(ref _virtualizationSummary, value);
    }

    public DelegateCommand OpenSecurityCommand { get; }
    public DelegateCommand OpenDevelopmentCommand { get; }
    public DelegateCommand OpenGameCommand { get; }
    public DelegateCommand OpenStartupCommand { get; }
    public DelegateCommand OpenCleanupCommand { get; }
    public DelegateCommand OpenNetworkCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    private async Task RefreshDataAsync()
    {
        var snapshot = await _systemControlService.GetStatusSnapshotAsync();
        var osVersion = string.IsNullOrWhiteSpace(snapshot.OSVersion) ? "Unknown OS" : snapshot.OSVersion;
        var machineName = string.IsNullOrWhiteSpace(snapshot.MachineName) ? "Unknown machine" : snapshot.MachineName;

        SystemInfoSummary = $"{osVersion} | {machineName} | CPU: {snapshot.CPUInfo}";
        SecuritySummary = snapshot.IsVbsEnabled
            ? "VBS 当前为开启状态，适合安全优先场景。"
            : "VBS 当前为关闭状态，适合性能优先场景。";
        VirtualizationSummary =
            $"Hyper-V: {FormatFeature(snapshot.HyperVState)} | WSL: {FormatFeature(snapshot.WslState)} | VMP: {FormatFeature(snapshot.VmPlatformState)} | HVCI: {(snapshot.IsHvciEnabled ? "On" : "Off")}";
        LastRefreshTime = $"Last refresh: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
    }

    private void NavigateToHub(string feature)
    {
        var parameters = new NavigationParameters
        {
            { "feature", feature }
        };

        _regionManager.RequestNavigate("ContentRegion", "OptimizeOption", parameters);
    }

    private static string FormatFeature(OptionalFeatureState state)
    {
        return state switch
        {
            OptionalFeatureState.Enabled => "On",
            OptionalFeatureState.Disabled => "Off",
            _ => "Unknown"
        };
    }
}
