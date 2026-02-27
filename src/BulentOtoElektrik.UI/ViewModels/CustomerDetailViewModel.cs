using System.Collections.ObjectModel;
using System.IO;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class CustomerDetailViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IExcelExportService _excelExportService;

    [ObservableProperty]
    private int _customerId;

    [ObservableProperty]
    private Customer? _customer;

    [ObservableProperty]
    private string _customerName = "";

    [ObservableProperty]
    private string? _phone1;

    [ObservableProperty]
    private string? _phone2;

    [ObservableProperty]
    private string? _identityNumber;

    [ObservableProperty]
    private string? _email;

    [ObservableProperty]
    private string? _address;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private ObservableCollection<Vehicle> _vehicles = new();

    [ObservableProperty]
    private Vehicle? _selectedVehicle;

    [ObservableProperty]
    private ObservableCollection<ServiceRecord> _serviceRecords = new();

    [ObservableProperty]
    private ObservableCollection<Payment> _payments = new();

    [ObservableProperty]
    private decimal _totalDebt;

    [ObservableProperty]
    private decimal _totalPayments;

    [ObservableProperty]
    private decimal _balance;

    [ObservableProperty]
    private bool _isLoading;

    public CustomerDetailViewModel(
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        IDialogService dialogService,
        IExcelExportService excelExportService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _excelExportService = excelExportService;
    }

    public async Task LoadAsync(int customerId)
    {
        CustomerId = customerId;
        IsLoading = true;

        try
        {
            var customer = await _unitOfWork.Customers.GetByIdWithDetailsAsync(customerId);
            if (customer == null)
            {
                await _dialogService.ShowMessageAsync("Musteri bulunamadi.", "Hata");
                _navigationService.GoBack();
                return;
            }

            Customer = customer;
            CustomerName = customer.FullName;
            Phone1 = customer.Phone1;
            Phone2 = customer.Phone2;
            IdentityNumber = customer.IdentityNumber;
            Email = customer.Email;
            Address = customer.Address;
            Notes = customer.Notes;

            Vehicles = new ObservableCollection<Vehicle>(customer.Vehicles);

            // Load payments
            var payments = await _unitOfWork.Payments.GetByCustomerIdAsync(customerId);
            Payments = new ObservableCollection<Payment>(payments);

            // Select first vehicle (or keep current selection) and force reload service records
            var vehicleToSelect = Vehicles.FirstOrDefault(v => v.Id == SelectedVehicle?.Id) ?? Vehicles.FirstOrDefault();
            SelectedVehicle = null; // force property change
            SelectedVehicle = vehicleToSelect;

            // Always reload service records even if same vehicle (EF tracking may reuse same object)
            if (SelectedVehicle != null)
            {
                await LoadServiceRecordsAsync(SelectedVehicle.Id);
            }

            RecalculateBalances();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Musteri bilgileri yuklenirken hata: {ex.Message}", "Hata");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedVehicleChanged(Vehicle? value)
    {
        if (value != null)
        {
            _ = LoadServiceRecordsAsync(value.Id);
        }
        else
        {
            ServiceRecords.Clear();
        }
    }

    private async Task LoadServiceRecordsAsync(int vehicleId)
    {
        try
        {
            var records = await _unitOfWork.ServiceRecords.GetByVehicleIdAsync(vehicleId);
            ServiceRecords = new ObservableCollection<ServiceRecord>(records);
        }
        catch
        {
            ServiceRecords.Clear();
        }
    }

    private void RecalculateBalances()
    {
        // Compute from in-memory collections to ensure fresh data
        TotalDebt = Vehicles.SelectMany(v => v.ServiceRecords ?? Enumerable.Empty<ServiceRecord>()).Sum(sr => sr.TotalAmount);
        TotalPayments = Payments.Sum(p => p.Amount);
        Balance = TotalDebt - TotalPayments;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsEditing = !IsEditing;
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (Customer == null) return;

        try
        {
            Customer.FullName = CustomerName;
            Customer.Phone1 = Phone1;
            Customer.Phone2 = Phone2;
            Customer.IdentityNumber = IdentityNumber;
            Customer.Email = Email;
            Customer.Address = Address;
            Customer.Notes = Notes;
            Customer.UpdatedAt = DateTime.Now;

            await _unitOfWork.Customers.UpdateAsync(Customer);
            await _unitOfWork.SaveChangesAsync();
            _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
            _ = _excelExportService.AutoExportReportsAsync(DateTime.Today);

            IsEditing = false;
            await _dialogService.ShowMessageAsync("Musteri bilgileri guncellendi.", "Basarili");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Kaydetme hatasi: {ex.Message}", "Hata");
        }
    }

    [RelayCommand]
    private async Task AddVehicleAsync()
    {
        var dialogVm = new AddVehicleDialogViewModel();
        var vehicle = await _dialogService.ShowDialogAsync<Vehicle>(dialogVm);
        if (vehicle != null)
        {
            try
            {
                vehicle.CustomerId = CustomerId;
                await _unitOfWork.Vehicles.AddAsync(vehicle);
                await _unitOfWork.SaveChangesAsync();
                await LoadAsync(CustomerId);
                _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
                _ = _excelExportService.AutoExportReportsAsync(DateTime.Today);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Arac eklenirken hata: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private async Task AddServiceAsync()
    {
        if (SelectedVehicle == null)
        {
            await _dialogService.ShowMessageAsync("Lutfen once bir arac secin.", "Uyari");
            return;
        }

        var technicians = await _unitOfWork.Technicians.GetActiveAsync();
        var dialogVm = new AddServiceDialogViewModel(technicians);
        var record = await _dialogService.ShowDialogAsync<ServiceRecord>(dialogVm);
        if (record != null)
        {
            try
            {
                record.VehicleId = SelectedVehicle.Id;
                record.TotalAmount = record.Quantity * record.UnitPrice;
                await _unitOfWork.ServiceRecords.AddAsync(record);
                await _unitOfWork.SaveChangesAsync();
                await LoadAsync(CustomerId);
                _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
                _ = _excelExportService.AutoExportReportsAsync(record.ServiceDate);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Islem eklenirken hata: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private async Task TakePaymentAsync()
    {
        var dialogVm = new PaymentDialogViewModel();
        var payment = await _dialogService.ShowDialogAsync<Payment>(dialogVm);
        if (payment != null)
        {
            try
            {
                payment.CustomerId = CustomerId;
                payment.VehicleId = SelectedVehicle?.Id;
                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();
                await LoadAsync(CustomerId);
                _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
                _ = _excelExportService.AutoExportReportsAsync(payment.PaymentDate);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Odeme kaydedilirken hata: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private async Task DeleteServiceAsync(ServiceRecord? record)
    {
        if (record == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Bu islem kaydini silmek istediginizden emin misiniz?",
            "Islem Sil");

        if (confirmed)
        {
            try
            {
                await _unitOfWork.ServiceRecords.DeleteAsync(record.Id);
                await _unitOfWork.SaveChangesAsync();
                await LoadAsync(CustomerId);
                _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
                _ = _excelExportService.AutoExportReportsAsync(record.ServiceDate);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Silme hatasi: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private async Task DeletePaymentAsync(Payment? payment)
    {
        if (payment == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Bu odeme kaydini silmek istediginizden emin misiniz?",
            "Odeme Sil");

        if (confirmed)
        {
            try
            {
                await _unitOfWork.Payments.DeleteAsync(payment.Id);
                await _unitOfWork.SaveChangesAsync();
                await LoadAsync(CustomerId);
                _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
                _ = _excelExportService.AutoExportReportsAsync(payment.PaymentDate);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Silme hatasi: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private async Task ExportAllReportsAsync()
    {
        try
        {
            _ = _excelExportService.AutoExportCustomerCardsAsync(CustomerId);
            _ = _excelExportService.AutoExportReportsAsync(DateTime.Today);
        }
        catch { }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (SelectedVehicle == null)
        {
            await _dialogService.ShowMessageAsync("Lütfen önce bir araç seçin.", "Uyarı");
            return;
        }

        try
        {
            var exportFolder = _excelExportService.GetExportFolder();
            Directory.CreateDirectory(exportFolder);

            // Sanitize file name (remove invalid chars from plate/name)
            var safeName = string.Join("_", $"{CustomerName}_{SelectedVehicle.PlateNumber}".Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeName}.xlsx";
            var filePath = Path.Combine(exportFolder, fileName);

            await _excelExportService.ExportCustomerCardAsync(
                CustomerId, SelectedVehicle.Id, filePath);
            await _dialogService.ShowMessageAsync($"Excel dosyası başarıyla oluşturuldu.\n{filePath}", "Başarılı");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Excel hatası: {ex.Message}", "Hata");
        }
    }
}
