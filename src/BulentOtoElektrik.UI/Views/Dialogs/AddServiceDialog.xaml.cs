using System.Windows;
using BulentOtoElektrik.UI.ViewModels.Dialogs;

namespace BulentOtoElektrik.UI.Views.Dialogs;

public partial class AddServiceDialog : Window
{
    public AddServiceDialog(AddServiceDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.CloseRequested += (result) =>
        {
            DialogResult = result;
            Close();
        };

        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName is nameof(AddServiceDialogViewModel.Quantity)
                or nameof(AddServiceDialogViewModel.UnitPrice))
            {
                UpdateTotal(viewModel);
            }
        };

        UpdateTotal(viewModel);
    }

    private void UpdateTotal(AddServiceDialogViewModel vm)
    {
        var total = vm.Quantity * vm.UnitPrice;
        TotalText.Text = total.ToString("C2", new System.Globalization.CultureInfo("tr-TR"));
    }
}
