using System.Windows;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.ViewModels.Dialogs;
using BulentOtoElektrik.UI.Views.Dialogs;

namespace BulentOtoElektrik.UI.Helpers;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string message, string title = "Onay")
    {
        var result = MessageBox.Show(
            message, title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public async Task ShowMessageAsync(string message, string title = "Bilgi")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public async Task<T?> ShowDialogAsync<T>(object viewModel) where T : class
    {
        Window? dialog = viewModel switch
        {
            AddCustomerDialogViewModel vm => new AddCustomerDialog(vm),
            AddVehicleDialogViewModel vm => new AddVehicleDialog(vm),
            AddServiceDialogViewModel vm => new AddServiceDialog(vm),
            PaymentDialogViewModel vm => new PaymentDialog(vm),
            AddExpenseDialogViewModel vm => CreateExpenseDialog(vm),
            _ => null
        };

        if (dialog == null) return default;

        dialog.Owner = Application.Current.MainWindow;
        var dialogResult = dialog.ShowDialog();

        if (dialogResult == true)
        {
            return viewModel switch
            {
                AddCustomerDialogViewModel vm => vm.CreatedCustomer as T,
                AddVehicleDialogViewModel vm => vm.CreatedVehicle as T,
                AddServiceDialogViewModel vm => vm.CreatedRecord as T,
                PaymentDialogViewModel vm => vm.CreatedPayment as T,
                AddExpenseDialogViewModel vm => vm.Result as T,
                _ => default
            };
        }

        return default;
    }

    private static Window CreateExpenseDialog(AddExpenseDialogViewModel vm)
    {
        var dialog = new AddExpenseDialog();
        dialog.DataContext = vm;
        return dialog;
    }
}
