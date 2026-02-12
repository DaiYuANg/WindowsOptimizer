using Prism.Commands;
using Prism.Mvvm;
using WindowsControlPanel.Service;

namespace WindowsControlPanel.ViewModels;

public class SecurityVirtualizationPageViewModel : BindableBase
{
    private readonly ISystemInfoService _systemInfoService;
    private readonly IRegionManager _regionManager;
    private string _vbsStatus = string.Empty;
    private string _adminStatus = string.Empty;
    private string _restartHint = string.Empty;
    private string _executionLog = string.Empty;

    public SecurityVirtualizationPageViewModel(
        ISystemInfoService systemInfoService,
        IRegionManager regionManager)
    {
        _systemInfoService = systemInfoService;
        _regionManager = regionManager;

        EnableDevModeCommand = new DelegateCommand(() =>
            AppendLog("已选择开发模式。后续可在这里接入 enable-dev.ps1 执行流程。"));
        EnableGameModeCommand = new DelegateCommand(() =>
            AppendLog("已选择游戏模式。后续可在这里接入 enable-game.ps1 执行流程。"));
        RefreshCommand = new DelegateCommand(RefreshStatus);
        BackToHomeCommand = new DelegateCommand(() =>
            _regionManager.RequestNavigate("ContentRegion", "Dashboard"));

        RefreshStatus();
    }

    public string VbsStatus
    {
        get => _vbsStatus;
        private set => SetProperty(ref _vbsStatus, value);
    }

    public string AdminStatus
    {
        get => _adminStatus;
        private set => SetProperty(ref _adminStatus, value);
    }

    public string RestartHint
    {
        get => _restartHint;
        private set => SetProperty(ref _restartHint, value);
    }

    public string ExecutionLog
    {
        get => _executionLog;
        private set => SetProperty(ref _executionLog, value);
    }

    public DelegateCommand EnableDevModeCommand { get; }
    public DelegateCommand EnableGameModeCommand { get; }
    public DelegateCommand RefreshCommand { get; }
    public DelegateCommand BackToHomeCommand { get; }

    private void RefreshStatus()
    {
        VbsStatus = _systemInfoService.IsVbsEnabled()
            ? "VBS 当前状态: 已开启"
            : "VBS 当前状态: 已关闭";

        // 这里只做展示占位，后续接入真实管理员权限检测。
        AdminStatus = "管理员权限: 待检测";
        RestartHint = "提示: 修改虚拟化能力后通常需要重启系统生效。";

        AppendLog("状态已刷新。");
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        ExecutionLog = string.IsNullOrEmpty(ExecutionLog)
            ? line
            : $"{ExecutionLog}{Environment.NewLine}{line}";
    }
}
