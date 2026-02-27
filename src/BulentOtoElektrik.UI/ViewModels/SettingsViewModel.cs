using System.Collections.ObjectModel;
using System.IO;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogService _dialogService;
    private readonly IExcelExportService _excelExportService;

    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private string _excelExportFolder = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _backups = new();
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = new();
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public SettingsViewModel(IBackupService backupService, IUnitOfWork unitOfWork, IDialogService dialogService, IExcelExportService excelExportService)
    {
        _backupService = backupService;
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
        _excelExportService = excelExportService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            BackupFolder = _backupService.GetBackupFolder();
            ExcelExportFolder = _excelExportService.GetExportFolder();

            var backupList = await _backupService.GetBackupsAsync();
            Backups = new ObservableCollection<string>(backupList);

            var categories = await _unitOfWork.ExpenseCategories.GetAllAsync();
            Categories = new ObservableCollection<ExpenseCategory>(categories);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ChangeExcelExportFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Excel dosyaları için klasör seçin",
            InitialDirectory = ExcelExportFolder
        };

        if (dialog.ShowDialog() == true)
        {
            _excelExportService.SetExportFolder(dialog.FolderName);
            ExcelExportFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void ChangeBackupFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Yedekleme klasörünü seçin",
            InitialDirectory = BackupFolder
        };

        if (dialog.ShowDialog() == true)
        {
            _backupService.SetBackupFolder(dialog.FolderName);
            BackupFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task ManualBackup()
    {
        IsBusy = true;
        try
        {
            var backupPath = await _backupService.CreateBackupAsync();
            if (!string.IsNullOrEmpty(backupPath))
            {
                var backupList = await _backupService.GetBackupsAsync();
                Backups = new ObservableCollection<string>(backupList);
                await _dialogService.ShowMessageAsync(
                    $"Yedekleme başarıyla oluşturuldu.\n\n{Path.GetFileName(backupPath)}",
                    "Yedekleme Başarılı");
            }
            else
            {
                await _dialogService.ShowMessageAsync(
                    "Yedeklenecek veritabanı dosyası bulunamadı.",
                    "Uyarı");
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Yedekleme sırasında hata oluştu:\n{ex.Message}",
                "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddCategory()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            await _dialogService.ShowMessageAsync("Kategori adı boş olamaz.", "Uyarı");
            return;
        }

        IsBusy = true;
        try
        {
            var category = new ExpenseCategory
            {
                Name = NewCategoryName.Trim(),
                IsActive = true
            };

            await _unitOfWork.ExpenseCategories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            Categories.Add(category);
            NewCategoryName = string.Empty;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Kategori eklenirken hata oluştu:\n{ex.Message}",
                "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleCategory(ExpenseCategory category)
    {
        if (category == null) return;

        IsBusy = true;
        try
        {
            category.IsActive = !category.IsActive;
            await _unitOfWork.ExpenseCategories.UpdateAsync(category);
            await _unitOfWork.SaveChangesAsync();

            // Refresh the list to reflect changes
            var index = Categories.IndexOf(category);
            if (index >= 0)
            {
                Categories[index] = category;
            }
        }
        catch (Exception ex)
        {
            category.IsActive = !category.IsActive; // revert
            await _dialogService.ShowMessageAsync(
                $"Kategori güncellenirken hata oluştu:\n{ex.Message}",
                "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
