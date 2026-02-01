using System.Windows;
using Wpf.Ui.Controls;

namespace WindowsControlPanel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IRegionManager _regionManager;
    
    public MainWindow(IRegionManager regionManager)
    {
        InitializeComponent();
        _regionManager = regionManager;
    }


    private void NavigationView_OnItemInvoked(object sender, RoutedEventArgs args)
    {
        throw new NotImplementedException();
    }
}