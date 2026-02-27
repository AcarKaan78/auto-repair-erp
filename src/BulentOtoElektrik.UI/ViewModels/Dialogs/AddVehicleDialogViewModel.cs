using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels.Dialogs;

public partial class AddVehicleDialogViewModel : ObservableObject
{
    private readonly IUnitOfWork? _unitOfWork;
    private readonly IDialogService? _dialogService;

    public AddVehicleDialogViewModel()
    {
    }

    public AddVehicleDialogViewModel(IUnitOfWork unitOfWork, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
    }

    public int CustomerId { get; set; }

    [ObservableProperty] private string _plateNumber = "";
    [ObservableProperty] private string? _vehicleBrand;
    [ObservableProperty] private string? _vehicleModel;
    [ObservableProperty] private int? _vehicleYear;

    public Vehicle? CreatedVehicle { get; private set; }

    public event Action<bool>? CloseRequested;

    partial void OnPlateNumberChanged(string value)
    {
        var formatted = PlateNumberFormatter.FormatPlate(value);
        if (formatted != value && !string.IsNullOrEmpty(formatted))
        {
            _plateNumber = formatted;
            OnPropertyChanged(nameof(PlateNumber));
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(PlateNumber))
        {
            if (_dialogService != null)
                await _dialogService.ShowMessageAsync("Plaka zorunludur.", "Uyarı");
            return;
        }

        if (!PlateNumberFormatter.ValidatePlate(PlateNumber))
        {
            if (_dialogService != null)
                await _dialogService.ShowMessageAsync("Geçerli bir plaka giriniz.", "Uyarı");
            return;
        }

        try
        {
            var vehicle = new Vehicle
            {
                CustomerId = CustomerId,
                PlateNumber = PlateNumberFormatter.FormatPlate(PlateNumber),
                VehicleBrand = VehicleBrand?.Trim(),
                VehicleModel = VehicleModel?.Trim(),
                VehicleYear = VehicleYear
            };

            if (_unitOfWork != null)
            {
                CreatedVehicle = await _unitOfWork.Vehicles.AddAsync(vehicle);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                CreatedVehicle = vehicle;
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
