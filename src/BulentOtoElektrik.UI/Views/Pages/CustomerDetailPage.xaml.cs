using System.Windows.Controls;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class CustomerDetailPage : UserControl
{
    public CustomerDetailPage()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is CustomerDetailViewModel vm && vm.CustomerId > 0)
            {
                await vm.LoadAsync(vm.CustomerId);
            }
        };
    }

    private void VehicleChip_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Chip chip && chip.DataContext is Vehicle vehicle)
        {
            if (DataContext is CustomerDetailViewModel vm)
            {
                vm.SelectedVehicle = vehicle;
            }
        }
    }
}
