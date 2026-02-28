using System.Windows;
using System.Windows.Controls;
using BulentOtoElektrik.UI.ViewModels;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class PersonnelPage : UserControl
{
    public PersonnelPage()
    {
        InitializeComponent();
        Loaded += PersonnelPage_Loaded;
    }

    private async void PersonnelPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PersonnelViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }
}
