using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels.Dialogs;

public partial class PaymentDialogViewModel : ObservableObject
{
    public PaymentDialogViewModel()
    {
    }

    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private CurrencyType _currency = CurrencyType.TL;
    [ObservableProperty] private PaymentMethod _paymentMethod = PaymentMethod.Cash;
    [ObservableProperty] private DateTime _paymentDate = DateTime.Today;
    [ObservableProperty] private string? _notes;

    public Payment? CreatedPayment { get; private set; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Save()
    {
        if (Amount <= 0)
            return;

        CreatedPayment = new Payment
        {
            Amount = Amount,
            Currency = Currency,
            PaymentMethod = PaymentMethod,
            PaymentDate = PaymentDate,
            Notes = Notes
        };

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
