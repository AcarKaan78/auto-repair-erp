using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class StokTakipPage : UserControl
{
    public StokTakipPage()
    {
        InitializeComponent();
        Loaded += StokTakipPage_Loaded;
    }

    private async void StokTakipPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StokTakipViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
