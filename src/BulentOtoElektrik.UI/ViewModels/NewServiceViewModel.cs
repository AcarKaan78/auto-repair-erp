using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class ServiceLineItem : ObservableObject
{
    [ObservableProperty] private string? _complaint;
    [ObservableProperty] private string _workPerformed = "";
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private CurrencyType _currency = CurrencyType.TL;

    public decimal LineTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));
}

public partial class NewServiceViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IExcelExportService _excelExportService;

    public NewServiceViewModel(
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        IDialogService dialogService,
        IExcelExportService excelExportService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _excelExportService = excelExportService;

        ServiceLines = new ObservableCollection<ServiceLineItem>();
        ServiceLines.CollectionChanged += OnServiceLinesChanged;
        SearchResults = new ObservableCollection<VehicleSearchResult>();
        Technicians = new ObservableCollection<Technician>();

        AddEmptyLine();
    }

    // --- Plate search & autocomplete ---

    [ObservableProperty]
    private string _plateText = "";

    [ObservableProperty]
    private bool _isSearchPopupOpen;

    private bool _suppressSearch;

    public ObservableCollection<VehicleSearchResult> SearchResults { get; }

    partial void OnPlateTextChanged(string value)
    {
        if (_suppressSearch) return;

        var formatted = PlateNumberFormatter.FormatPlate(value);
        if (formatted != value && !string.IsNullOrEmpty(formatted))
        {
            _plateText = formatted;
            OnPropertyChanged(nameof(PlateText));
        }

        _ = SearchVehiclesAsync(value);
    }

    private async Task SearchVehiclesAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            SearchResults.Clear();
            IsSearchPopupOpen = false;
            return;
        }

        try
        {
            var results = await _unitOfWork.Vehicles.SearchAsync(searchTerm);
            SearchResults.Clear();
            foreach (var r in results)
                SearchResults.Add(r);

            IsSearchPopupOpen = SearchResults.Count > 0;
        }
        catch
        {
            SearchResults.Clear();
            IsSearchPopupOpen = false;
        }
    }

    // --- Selected vehicle & customer info ---

    [ObservableProperty]
    private Vehicle? _selectedVehicle;

    [ObservableProperty]
    private string _customerDisplayName = "";

    [ObservableProperty]
    private string _vehicleDisplayInfo = "";

    [ObservableProperty]
    private ObservableCollection<ServiceRecord> _pastServiceRecords = new();

    [RelayCommand]
    private async Task SelectSearchResult(VehicleSearchResult? result)
    {
        if (result == null) return;

        _suppressSearch = true;
        IsSearchPopupOpen = false;
        SearchResults.Clear();

        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(result.VehicleId);
        if (vehicle != null)
        {
            SelectedVehicle = vehicle;
            PlateText = vehicle.PlateNumber;

            // Load customer info
            var customer = await _unitOfWork.Customers.GetByIdAsync(vehicle.CustomerId);
            if (customer != null)
            {
                CustomerDisplayName = customer.FullName;
                VehicleDisplayInfo = $"{vehicle.VehicleBrand} {vehicle.VehicleModel} ({vehicle.VehicleYear})";
            }

            // Load past service records
            await LoadPastServiceRecordsAsync(vehicle.Id);
        }
        _suppressSearch = false;
    }

    private async Task LoadPastServiceRecordsAsync(int vehicleId)
    {
        try
        {
            var records = await _unitOfWork.ServiceRecords.GetByVehicleIdAsync(vehicleId);
            PastServiceRecords = new ObservableCollection<ServiceRecord>(records);
        }
        catch
        {
            PastServiceRecords.Clear();
        }
    }

    // --- Service Lines ---

    public ObservableCollection<ServiceLineItem> ServiceLines { get; }

    [ObservableProperty]
    private decimal _grandTotal;

    private void OnServiceLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (ServiceLineItem item in e.OldItems)
                item.PropertyChanged -= OnLineItemPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (ServiceLineItem item in e.NewItems)
                item.PropertyChanged += OnLineItemPropertyChanged;
        }
        RecalculateGrandTotal();
    }

    private void OnLineItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ServiceLineItem.LineTotal))
            RecalculateGrandTotal();
    }

    private void RecalculateGrandTotal()
    {
        GrandTotal = ServiceLines.Sum(l => l.LineTotal);
    }

    private void AddEmptyLine()
    {
        ServiceLines.Add(new ServiceLineItem { Currency = DefaultCurrency });
    }

    [RelayCommand]
    private void AddLine()
    {
        AddEmptyLine();
    }

    [RelayCommand]
    private void RemoveLine(ServiceLineItem? item)
    {
        if (item != null && ServiceLines.Count > 1)
        {
            ServiceLines.Remove(item);
        }
    }

    // --- Date and Currency ---

    [ObservableProperty]
    private DateTime _serviceDate = DateTime.Today;

    [ObservableProperty]
    private CurrencyType _defaultCurrency = CurrencyType.TL;

    // --- Technicians ---

    public ObservableCollection<Technician> Technicians { get; }

    // --- Initialization ---

    public async Task InitializeAsync()
    {
        try
        {
            var technicians = await _unitOfWork.Technicians.GetActiveAsync();
            Technicians.Clear();
            foreach (var t in technicians)
                Technicians.Add(t);
        }
        catch
        {
            // Silently handle initialization errors
        }
    }

    // --- Add New Customer & Vehicle ---

    [RelayCommand]
    private async Task AddNewCustomerAndVehicle()
    {
        var dialogVm = new Dialogs.AddCustomerDialogViewModel(_unitOfWork, _dialogService);
        var customer = await _dialogService.ShowDialogAsync<Customer>(dialogVm);

        if (customer == null) return;

        // Load the vehicle that was created together with the customer
        var vehicles = await _unitOfWork.Vehicles.GetByCustomerIdAsync(customer.Id);
        var vehicle = vehicles.FirstOrDefault();

        if (vehicle != null)
        {
            _suppressSearch = true;
            SelectedVehicle = vehicle;
            PlateText = vehicle.PlateNumber;
            CustomerDisplayName = customer.FullName;
            VehicleDisplayInfo = $"{vehicle.VehicleBrand} {vehicle.VehicleModel} ({vehicle.VehicleYear})";
            _ = _excelExportService.AutoExportCustomerCardsAsync(customer.Id);
            _ = _excelExportService.AutoExportReportsAsync(DateTime.Today);
            _suppressSearch = false;
        }
    }

    // --- Save ---

    [RelayCommand]
    private async Task Save()
    {
        // Validation
        if (SelectedVehicle == null)
        {
            await _dialogService.ShowMessageAsync("Lütfen bir araç seçiniz.", "Uyarı");
            return;
        }

        var validLines = ServiceLines
            .Where(l => !string.IsNullOrWhiteSpace(l.WorkPerformed) && l.UnitPrice > 0)
            .ToList();

        if (validLines.Count == 0)
        {
            await _dialogService.ShowMessageAsync(
                "En az bir satırda 'Yapılan İşlem' ve 'Birim Fiyat' girilmelidir.", "Uyarı");
            return;
        }

        try
        {
            var records = validLines.Select(line => new ServiceRecord
            {
                VehicleId = SelectedVehicle.Id,
                TechnicianId = line.SelectedTechnician?.Id,
                ServiceDate = ServiceDate,
                Complaint = line.Complaint,
                WorkPerformed = line.WorkPerformed,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                TotalAmount = line.LineTotal,
                Currency = line.Currency
            }).ToList();

            await _unitOfWork.ServiceRecords.AddRangeAsync(records);
            await _unitOfWork.SaveChangesAsync();
            _ = _excelExportService.AutoExportCustomerCardsAsync(SelectedVehicle.CustomerId);
            _ = _excelExportService.AutoExportReportsAsync(ServiceDate);

            var goToCustomer = await _dialogService.ShowConfirmationAsync(
                "İşlem kaydı başarıyla oluşturuldu.\n\nMüşteri kartına gitmek ister misiniz?",
                "Başarılı");

            if (goToCustomer)
            {
                _navigationService.NavigateToCustomerDetail(SelectedVehicle.CustomerId);
            }
            else
            {
                // Reset form for another service
                ResetForm();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Kayıt sırasında hata oluştu: {ex.Message}", "Hata");
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _navigationService.GoBack();
    }

    private void ResetForm()
    {
        SelectedVehicle = null;
        PlateText = "";
        CustomerDisplayName = "";
        VehicleDisplayInfo = "";
        ServiceDate = DateTime.Today;
        ServiceLines.Clear();
        AddEmptyLine();
        GrandTotal = 0;
        PastServiceRecords.Clear();
    }
}
