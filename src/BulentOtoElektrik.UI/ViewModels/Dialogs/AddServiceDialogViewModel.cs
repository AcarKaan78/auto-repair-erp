using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels.Dialogs;

public partial class AddServiceDialogViewModel : ObservableObject
{
    public AddServiceDialogViewModel()
    {
        Technicians = new ObservableCollection<Technician>();
        StockItems = new ObservableCollection<StockItem>();
    }

    public AddServiceDialogViewModel(List<Technician> technicians)
    {
        Technicians = new ObservableCollection<Technician>(technicians);
        StockItems = new ObservableCollection<StockItem>();
    }

    public AddServiceDialogViewModel(List<Technician> technicians, List<StockItem> stockItems)
    {
        Technicians = new ObservableCollection<Technician>(technicians);
        StockItems = new ObservableCollection<StockItem>(stockItems);
    }

    public ObservableCollection<Technician> Technicians { get; }
    public ObservableCollection<StockItem> StockItems { get; }

    [ObservableProperty] private string? _complaint;
    [ObservableProperty] private string _workPerformed = "";
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private CurrencyType _currency = CurrencyType.TL;
    [ObservableProperty] private DateTime _serviceDate = DateTime.Today;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private StockItem? _selectedStockItem;
    [ObservableProperty] private int _materialQuantityUsed;

    public ServiceRecord? CreatedRecord { get; private set; }

    public event Action<bool>? CloseRequested;

    public string? ValidationError { get; private set; }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(WorkPerformed))
        {
            ValidationError = "Yapılan İşlem alanı zorunludur.";
            OnPropertyChanged(nameof(ValidationError));
            return;
        }
        if (UnitPrice <= 0)
        {
            ValidationError = "Birim Fiyat 0'dan büyük olmalıdır.";
            OnPropertyChanged(nameof(ValidationError));
            return;
        }

        CreatedRecord = new ServiceRecord
        {
            Complaint = Complaint,
            WorkPerformed = WorkPerformed,
            TechnicianId = SelectedTechnician?.Id,
            Quantity = Quantity,
            UnitPrice = UnitPrice,
            TotalAmount = Quantity * UnitPrice,
            Currency = Currency,
            ServiceDate = ServiceDate,
            Notes = Notes,
            StockItemId = SelectedStockItem?.Id,
            MaterialQuantityUsed = MaterialQuantityUsed
        };

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
