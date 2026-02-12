using Prism.Commands;
using Prism.Mvvm;
using WindowsControlPanel.Service;

namespace WindowsControlPanel.ViewModels;

public class DashboardPageViewModel : BindableBase
{
    private readonly ISystemInfoService _systemInfoService;
    private readonly IRegionManager _regionManager;
    private string _systemInfoSummary = "Loading system information...";
    private string _lastRefreshTime = string.Empty;
    private string _securitySummary = string.Empty;

    public DashboardPageViewModel(
        ISystemInfoService systemInfoService,
        IRegionManager regionManager)
    {
        _systemInfoService = systemInfoService;
        _regionManager = regionManager;

        OpenSecurityCommand = new DelegateCommand(() => NavigateToHub("security"));
        OpenDevelopmentCommand = new DelegateCommand(() => NavigateToHub("development"));
        OpenGameCommand = new DelegateCommand(() => NavigateToHub("gaming"));
        OpenStartupCommand = new DelegateCommand(() => NavigateToHub("startup"));
        OpenCleanupCommand = new DelegateCommand(() => NavigateToHub("cleanup"));
        OpenNetworkCommand = new DelegateCommand(() => NavigateToHub("network"));
        RefreshCommand = new DelegateCommand(RefreshData);

        RefreshData();
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

    public DelegateCommand OpenSecurityCommand { get; }
    public DelegateCommand OpenDevelopmentCommand { get; }
    public DelegateCommand OpenGameCommand { get; }
    public DelegateCommand OpenStartupCommand { get; }
    public DelegateCommand OpenCleanupCommand { get; }
    public DelegateCommand OpenNetworkCommand { get; }
    public DelegateCommand RefreshCommand { get; }

    private void RefreshData()
    {
        var osVersion = string.IsNullOrWhiteSpace(_systemInfoService.OSVersion) ? "Unknown OS" : _systemInfoService.OSVersion;
        var machineName = string.IsNullOrWhiteSpace(_systemInfoService.MachineName) ? "Unknown machine" : _systemInfoService.MachineName;

        SystemInfoSummary = $"{osVersion} | {machineName}";
        SecuritySummary = _systemInfoService.IsVbsEnabled()
            ? "VBS 当前为开启状态，适合安全优先场景。"
            : "VBS 当前为关闭状态，适合性能优先场景。";
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
}
