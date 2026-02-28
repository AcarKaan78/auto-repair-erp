using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class PersonnelViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private ObservableCollection<Personnel> _personnelList = new();
    [ObservableProperty] private Personnel? _selectedPersonnel;
    [ObservableProperty] private string _newFullName = string.Empty;
    [ObservableProperty] private string _newTcKimlikNo = string.Empty;
    [ObservableProperty] private string _newPhone = string.Empty;
    [ObservableProperty] private string _newRole = string.Empty;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isBusy;

    // Edit form fields
    [ObservableProperty] private string _editFullName = string.Empty;
    [ObservableProperty] private string _editTcKimlikNo = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editRole = string.Empty;

    public PersonnelViewModel(IUnitOfWork unitOfWork, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadPersonnelAsync();
    }

    private async Task LoadPersonnelAsync()
    {
        IsBusy = true;
        try
        {
            var list = await _unitOfWork.Personnel.GetAllAsync();
            PersonnelList = new ObservableCollection<Personnel>(list);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddPersonnel()
    {
        if (string.IsNullOrWhiteSpace(NewFullName))
        {
            await _dialogService.ShowMessageAsync("Ad Soyad boş olamaz.", "Uyarı");
            return;
        }

        IsBusy = true;
        try
        {
            var personnel = new Personnel
            {
                FullName = NewFullName.Trim(),
                TcKimlikNo = string.IsNullOrWhiteSpace(NewTcKimlikNo) ? null : NewTcKimlikNo.Trim(),
                Phone = string.IsNullOrWhiteSpace(NewPhone) ? null : NewPhone.Trim(),
                Role = string.IsNullOrWhiteSpace(NewRole) ? null : NewRole.Trim(),
                IsActive = true
            };

            var created = await _unitOfWork.Personnel.AddAsync(personnel);
            await _unitOfWork.SaveChangesAsync();
            PersonnelList.Add(created);

            NewFullName = string.Empty;
            NewTcKimlikNo = string.Empty;
            NewPhone = string.Empty;
            NewRole = string.Empty;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Personel eklenirken hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleActive(Personnel personnel)
    {
        if (personnel == null) return;

        IsBusy = true;
        try
        {
            personnel.IsActive = !personnel.IsActive;
            await _unitOfWork.Personnel.UpdateAsync(personnel);
            await _unitOfWork.SaveChangesAsync();
            await LoadPersonnelAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Durum güncellenirken hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeletePersonnel(Personnel personnel)
    {
        if (personnel == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"'{personnel.FullName}' personelini silmek istediğinizden emin misiniz?",
            "Personel Sil");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            await _unitOfWork.Personnel.DeleteAsync(personnel.Id);
            await _unitOfWork.SaveChangesAsync();
            await LoadPersonnelAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Silme hatası: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Save()
    {
        if (SelectedPersonnel == null) return;

        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            await _dialogService.ShowMessageAsync("Ad Soyad boş olamaz.", "Uyarı");
            return;
        }

        IsBusy = true;
        try
        {
            SelectedPersonnel.FullName = EditFullName.Trim();
            SelectedPersonnel.TcKimlikNo = string.IsNullOrWhiteSpace(EditTcKimlikNo) ? null : EditTcKimlikNo.Trim();
            SelectedPersonnel.Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim();
            SelectedPersonnel.Role = string.IsNullOrWhiteSpace(EditRole) ? null : EditRole.Trim();

            await _unitOfWork.Personnel.UpdateAsync(SelectedPersonnel);
            await _unitOfWork.SaveChangesAsync();
            IsEditing = false;
            SelectedPersonnel = null;
            await LoadPersonnelAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Kaydetme sırasında hata oluştu: {ex.Message}", "Hata");
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
        SelectedPersonnel = null;
    }

    [RelayCommand]
    private void EditPersonnel(Personnel personnel)
    {
        if (personnel == null) return;
        SelectedPersonnel = personnel;
        EditFullName = personnel.FullName;
        EditTcKimlikNo = personnel.TcKimlikNo ?? string.Empty;
        EditPhone = personnel.Phone ?? string.Empty;
        EditRole = personnel.Role ?? string.Empty;
        IsEditing = true;
    }
}
