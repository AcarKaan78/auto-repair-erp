using System.Windows;
using BulentOtoElektrik.UI.ViewModels.Dialogs;

namespace BulentOtoElektrik.UI.Views.Dialogs;

public partial class AddCustomerDialog : Window
{
    public AddCustomerDialog(AddCustomerDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseRequested += (result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
