using System.Windows.Controls;
using System.Windows.Input;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class CustomerSearchPage : UserControl
{
    public CustomerSearchPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CustomerSearchViewModel vm)
            {
                await vm.LoadRecentCustomersAsync();
            }
        };
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is VehicleSearchResult result)
        {
            if (DataContext is CustomerSearchViewModel vm)
            {
                vm.ViewCustomerCommand.Execute(result);
            }
        }
    }
}
