using Prism.Commands;
using Prism.Mvvm;
using WindowsControlPanel.Service;

namespace WindowsControlPanel.ViewModels;

public class OptimizeOptionPageViewModel : BindableBase, INavigationAware
{
    private readonly ISystemInfoService _systemInfoService;
    private readonly IRegionManager _regionManager;
    private string _globalStatus = string.Empty;
    private bool _isSecuritySelected;
    private bool _isDevelopmentSelected;
    private bool _isGameSelected;
    private bool _isStartupSelected;
    private bool _isCleanupSelected;
    private bool _isNetworkSelected;

    public OptimizeOptionPageViewModel(
        ISystemInfoService systemInfoService,
        IRegionManager regionManager)
    {
        _systemInfoService = systemInfoService;
        _regionManager = regionManager;

        OpenSecurityCommand = new DelegateCommand(() => NavigateFeature("security"));
        OpenDevelopmentCommand = new DelegateCommand(() => NavigateFeature("development"));
        OpenGameCommand = new DelegateCommand(() => NavigateFeature("gaming"));
        OpenStartupCommand = new DelegateCommand(() => NavigateFeature("startup"));
        OpenCleanupCommand = new DelegateCommand(() => NavigateFeature("cleanup"));
        OpenNetworkCommand = new DelegateCommand(() => NavigateFeature("network"));
        BackToHomeCommand = new DelegateCommand(() =>
            _regionManager.RequestNavigate("ContentRegion", "Dashboard"));

        RefreshStatus();
    }

    public string GlobalStatus
    {
        get => _globalStatus;
        private set => SetProperty(ref _globalStatus, value);
    }

    public bool IsSecuritySelected
    {
        get => _isSecuritySelected;
        private set => SetProperty(ref _isSecuritySelected, value);
    }

    public bool IsDevelopmentSelected
    {
        get => _isDevelopmentSelected;
        private set => SetProperty(ref _isDevelopmentSelected, value);
    }

    public bool IsGameSelected
    {
        get => _isGameSelected;
        private set => SetProperty(ref _isGameSelected, value);
    }

    public bool IsStartupSelected
    {
        get => _isStartupSelected;
        private set => SetProperty(ref _isStartupSelected, value);
    }

    public bool IsCleanupSelected
    {
        get => _isCleanupSelected;
        private set => SetProperty(ref _isCleanupSelected, value);
    }

    public bool IsNetworkSelected
    {
        get => _isNetworkSelected;
        private set => SetProperty(ref _isNetworkSelected, value);
    }

    public DelegateCommand OpenSecurityCommand { get; }
    public DelegateCommand OpenDevelopmentCommand { get; }
    public DelegateCommand OpenGameCommand { get; }
    public DelegateCommand OpenStartupCommand { get; }
    public DelegateCommand OpenCleanupCommand { get; }
    public DelegateCommand OpenNetworkCommand { get; }
    public DelegateCommand BackToHomeCommand { get; }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var feature = navigationContext.Parameters.GetValue<string>("feature");
        NavigateFeature(string.IsNullOrWhiteSpace(feature) ? "security" : feature);
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    private void RefreshStatus()
    {
        var vbsEnabled = _systemInfoService.IsVbsEnabled();
        GlobalStatus = vbsEnabled
            ? "当前系统为安全优先状态（VBS 已开启）"
            : "当前系统为性能优先状态（VBS 已关闭）";
    }

    private void NavigateFeature(string feature)
    {
        RefreshStatus();
        ApplySelection(feature);

        if (feature == "security")
        {
            _regionManager.RequestNavigate("FeatureRegion", "SecurityVirtualization");
            return;
        }

        var parameters = new NavigationParameters
        {
            { "feature", feature }
        };

        _regionManager.RequestNavigate("FeatureRegion", "FeaturePlaceholder", parameters);
    }

    private void ApplySelection(string feature)
    {
        IsSecuritySelected = feature == "security";
        IsDevelopmentSelected = feature == "development";
        IsGameSelected = feature == "gaming";
        IsStartupSelected = feature == "startup";
        IsCleanupSelected = feature == "cleanup";
        IsNetworkSelected = feature == "network";
    }
}
