using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels.Dialogs;

public partial class AddCustomerDialogViewModel : ObservableObject
{
    private readonly IUnitOfWork? _unitOfWork;
    private readonly IDialogService? _dialogService;

    public AddCustomerDialogViewModel()
    {
    }

    public AddCustomerDialogViewModel(IUnitOfWork unitOfWork, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
    }

    // Customer fields
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string? _phone1;
    [ObservableProperty] private string? _phone2;
    [ObservableProperty] private string? _identityNumber;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _address;

    // Vehicle fields
    [ObservableProperty] private string _plateNumber = "";
    [ObservableProperty] private string? _vehicleBrand;
    [ObservableProperty] private string? _vehicleModel;
    [ObservableProperty] private int? _vehicleYear;

    public Customer? CreatedCustomer { get; private set; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            if (_dialogService != null)
                await _dialogService.ShowMessageAsync("Müşteri adı zorunludur.", "Uyarı");
            return;
        }

        if (string.IsNullOrWhiteSpace(PlateNumber))
        {
            if (_dialogService != null)
                await _dialogService.ShowMessageAsync("Plaka zorunludur.", "Uyarı");
            return;
        }

        try
        {
            var customer = new Customer
            {
                FullName = FullName.Trim(),
                Phone1 = Phone1?.Trim(),
                Phone2 = Phone2?.Trim(),
                IdentityNumber = IdentityNumber?.Trim(),
                Email = Email?.Trim(),
                Address = Address?.Trim()
            };

            if (_unitOfWork != null)
            {
                CreatedCustomer = await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();

                // Create the vehicle linked to this customer
                var vehicle = new Vehicle
                {
                    CustomerId = CreatedCustomer.Id,
                    PlateNumber = PlateNumberFormatter.FormatPlate(PlateNumber),
                    VehicleBrand = VehicleBrand?.Trim(),
                    VehicleModel = VehicleModel?.Trim(),
                    VehicleYear = VehicleYear
                };
                await _unitOfWork.Vehicles.AddAsync(vehicle);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                CreatedCustomer = customer;
            }

            CloseRequested?.Invoke(true);
        }
        catch (Exception ex)
        {
            if (_dialogService != null)
                await _dialogService.ShowMessageAsync($"Kayıt hatası: {ex.Message}", "Hata");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
