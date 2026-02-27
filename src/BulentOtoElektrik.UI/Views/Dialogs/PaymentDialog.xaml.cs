using System.Windows;
using BulentOtoElektrik.UI.ViewModels.Dialogs;

namespace BulentOtoElektrik.UI.Views.Dialogs;

public partial class PaymentDialog : Window
{
    public PaymentDialog(PaymentDialogViewModel viewModel)
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
