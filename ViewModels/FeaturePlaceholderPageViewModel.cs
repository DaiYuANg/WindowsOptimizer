using Prism.Mvvm;

namespace WindowsControlPanel.ViewModels;

public class FeaturePlaceholderPageViewModel : BindableBase, INavigationAware
{
    private string _title = "模块开发中";
    private string _description = "该模块尚未接入具体功能。";

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        private set => SetProperty(ref _description, value);
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        var feature = navigationContext.Parameters.GetValue<string>("feature");
        switch (feature)
        {
            case "development":
                Title = "开发环境";
                Description = "管理 WSL、虚拟机平台和开发者模式相关能力。";
                break;
            case "gaming":
                Title = "游戏优化";
                Description = "围绕虚拟化冲突、服务裁剪和性能预设进行调整。";
                break;
            case "startup":
                Title = "启动与服务";
                Description = "管理启动项、服务和后台任务的加载行为。";
                break;
            case "cleanup":
                Title = "磁盘与清理";
                Description = "清理缓存、临时文件并执行磁盘空间回收。";
                break;
            case "network":
                Title = "网络与 DNS";
                Description = "优化 DNS、网络协议栈与连接策略。";
                break;
            default:
                Title = "模块开发中";
                Description = "该模块尚未接入具体功能。";
                break;
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }
}
