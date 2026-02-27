using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class NewServicePage : UserControl
{
    public NewServicePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is NewServiceViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void SearchResultItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.DataContext is VehicleSearchResult result)
        {
            if (DataContext is NewServiceViewModel vm)
            {
                vm.SelectSearchResultCommand.Execute(result);
            }
        }
    }
}
