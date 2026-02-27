using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
        Loaded += ReportsPage_Loaded;
    }

    private async void ReportsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
