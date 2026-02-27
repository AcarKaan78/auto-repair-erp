using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void RecentServices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is ServiceRecord record)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToServiceCustomerCommand.Execute(record);
            }
        }
    }

    private void TopDebtor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListViewItem item && item.DataContext is Customer customer)
        {
            if (DataContext is DashboardViewModel vm)
            {
                vm.NavigateToCustomerCommand.Execute(customer);
            }
        }
    }

    private void InnerControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Bubble the scroll event to the parent ScrollViewer
        e.Handled = true;
        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        if (sender is UIElement element)
        {
            element.RaiseEvent(eventArg);
        }
    }
}
