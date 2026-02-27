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
    }

    public AddServiceDialogViewModel(List<Technician> technicians)
    {
        Technicians = new ObservableCollection<Technician>(technicians);
    }

    public ObservableCollection<Technician> Technicians { get; }

    [ObservableProperty] private string? _complaint;
    [ObservableProperty] private string _workPerformed = "";
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private int _quantity = 1;
    [ObservableProperty] private decimal _unitPrice;
    [ObservableProperty] private CurrencyType _currency = CurrencyType.TL;
    [ObservableProperty] private DateTime _serviceDate = DateTime.Today;
    [ObservableProperty] private string? _notes;

    public ServiceRecord? CreatedRecord { get; private set; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(WorkPerformed) || UnitPrice <= 0)
            return;

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
