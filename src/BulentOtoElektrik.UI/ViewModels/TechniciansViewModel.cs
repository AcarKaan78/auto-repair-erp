using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class TechniciansViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private ObservableCollection<Technician> _technicians = new();
    [ObservableProperty] private Technician? _selectedTechnician;
    [ObservableProperty] private string _newTechnicianName = string.Empty;
    [ObservableProperty] private string _newTechnicianPhone = string.Empty;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isBusy;

    // Edit form fields
    [ObservableProperty] private string _editFullName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;

    public TechniciansViewModel(IUnitOfWork unitOfWork, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadTechniciansAsync();
    }

    private async Task LoadTechniciansAsync()
    {
        IsBusy = true;
        try
        {
            var technicians = await _unitOfWork.Technicians.GetAllAsync();
            Technicians = new ObservableCollection<Technician>(technicians);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddTechnician()
    {
        if (string.IsNullOrWhiteSpace(NewTechnicianName))
        {
            await _dialogService.ShowMessageAsync(
                "Teknisyen adı boş olamaz.", "Uyarı");
            return;
        }

        IsBusy = true;
        try
        {
            var technician = new Technician
            {
                FullName = NewTechnicianName.Trim(),
                Phone = string.IsNullOrWhiteSpace(NewTechnicianPhone) ? null : NewTechnicianPhone.Trim(),
                IsActive = true
            };

            var created = await _unitOfWork.Technicians.AddAsync(technician);
            await _unitOfWork.SaveChangesAsync();
            Technicians.Add(created);

            NewTechnicianName = string.Empty;
            NewTechnicianPhone = string.Empty;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Teknisyen eklenirken hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActive(Technician technician)
    {
        if (technician == null) return;

        IsBusy = true;
        try
        {
            technician.IsActive = !technician.IsActive;
            await _unitOfWork.Technicians.UpdateAsync(technician);
            await _unitOfWork.SaveChangesAsync();

            // Refresh the collection to update the UI
            await LoadTechniciansAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Durum güncellenirken hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedTechnician == null) return;

        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            await _dialogService.ShowMessageAsync("Teknisyen adı boş olamaz.", "Uyarı");
            return;
        }

        IsBusy = true;
        try
        {
            SelectedTechnician.FullName = EditFullName.Trim();
            SelectedTechnician.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();

            await _unitOfWork.Technicians.UpdateAsync(SelectedTechnician);
            await _unitOfWork.SaveChangesAsync();
            IsEditing = false;
            SelectedTechnician = null;
            await LoadTechniciansAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Kaydetme sırasında hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        SelectedTechnician = null;
    }

    [RelayCommand]
    private void EditTechnician(Technician technician)
    {
        if (technician == null) return;
        SelectedTechnician = technician;
        EditFullName = technician.FullName;
        EditPhone = technician.Phone ?? string.Empty;
        IsEditing = true;
    }
}
