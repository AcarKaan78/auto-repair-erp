using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class DailyExpensesPage : UserControl
{
    public DailyExpensesPage()
    {
        InitializeComponent();
        Loaded += DailyExpensesPage_Loaded;
    }

    private async void DailyExpensesPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DailyExpensesViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
