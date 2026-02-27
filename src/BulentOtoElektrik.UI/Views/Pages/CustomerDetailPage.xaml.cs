using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

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
}
