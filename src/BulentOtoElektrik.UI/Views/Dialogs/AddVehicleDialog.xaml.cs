using System.Windows;
using BulentOtoElektrik.UI.ViewModels.Dialogs;

namespace BulentOtoElektrik.UI.Views.Dialogs;

public partial class AddVehicleDialog : Window
{
    public AddVehicleDialog(AddVehicleDialogViewModel viewModel)
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
