using System.Windows;
using System.Windows.Controls;

namespace WindowsOptimizer.Views;

public partial class OptimizeOptionPage
{
    private void BtnDetail_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            MessageBox.Show("这里展示更详细的操作说明和可能的副作用。", "功能详情", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}