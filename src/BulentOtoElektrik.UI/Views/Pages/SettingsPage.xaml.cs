using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
