using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class TechniciansPage : UserControl
{
    public TechniciansPage()
    {
        InitializeComponent();
        Loaded += TechniciansPage_Loaded;
    }

    private async void TechniciansPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TechniciansViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
