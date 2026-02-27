using System.Windows;
using BulentOtoElektrik.UI.ViewModels.Dialogs;

namespace BulentOtoElektrik.UI.Views.Dialogs;

public partial class AddExpenseDialog : Window
{
    public AddExpenseDialog()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddExpenseDialogViewModel vm)
        {
            vm.SaveCommand.Execute(null);
            if (vm.Result != null)
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
