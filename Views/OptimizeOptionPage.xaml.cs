using System.Windows.Controls;
using Serilog;
using WindowsControlPanel.ViewModels;

namespace WindowsControlPanel.Views;

public partial class OptimizeOptionPage : UserControl
{
    public OptimizeOptionPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is OptimizeOptionPageViewModel vm)
        {
            Log.Logger.Information("OptimizeOptionPage loaded. Triggering EnsureFeatureRegionReady.");
            vm.EnsureFeatureRegionReady();
            return;
        }

        Log.Logger.Warning(
            "OptimizeOptionPage loaded but DataContext is null or unexpected. DataContextType={DataContextType}",
            DataContext?.GetType().FullName ?? "<null>"
        );
    }
}
