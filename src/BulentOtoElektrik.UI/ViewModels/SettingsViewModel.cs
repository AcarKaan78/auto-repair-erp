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
    private readonly IExcelImportService _excelImportService;

    [ObservableProperty] private string _backupFolder = string.Empty;
    [ObservableProperty] private string _excelExportFolder = string.Empty;
    [ObservableProperty] private bool _isAutoExportEnabled;
    [ObservableProperty] private ObservableCollection<string> _backups = new();
    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = new();
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _importStatusText = string.Empty;
    [ObservableProperty] private int _importProgress;
    [ObservableProperty] private bool _isImporting;

    public bool IsNotImporting => !IsImporting;

    partial void OnIsImportingChanged(bool value) => OnPropertyChanged(nameof(IsNotImporting));

    public SettingsViewModel(
        IBackupService backupService,
        IUnitOfWork unitOfWork,
        IDialogService dialogService,
        IExcelExportService excelExportService,
        IExcelImportService excelImportService)
    {
        _backupService = backupService;
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
        _excelExportService = excelExportService;
        _excelImportService = excelImportService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            BackupFolder = _backupService.GetBackupFolder();
            ExcelExportFolder = _excelExportService.GetExportFolder();
            IsAutoExportEnabled = _excelExportService.IsAutoExportEnabled;

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
    private async Task ToggleAutoExport()
    {
        if (!IsAutoExportEnabled)
        {
            // Turning OFF
            _excelExportService.SetAutoExportEnabled(false);
            return;
        }

        // Turning ON — check if export folder has been configured
        var settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "export_settings.txt");
        if (!File.Exists(settingsFile))
        {
            // First time: ask for folder
            await _dialogService.ShowMessageAsync(
                "Excel dosyalarını kaydetmek istediğiniz klasörü seçin.",
                "Klasör Seçimi");

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Excel dosyaları için kayıt klasörünü seçin"
            };

            if (dialog.ShowDialog() == true)
            {
                _excelExportService.SetExportFolder(dialog.FolderName);
                File.WriteAllText(settingsFile, dialog.FolderName);
                ExcelExportFolder = dialog.FolderName;
            }
            else
            {
                // User cancelled — keep disabled
                IsAutoExportEnabled = false;
                return;
            }
        }

        _excelExportService.SetAutoExportEnabled(true);
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
            var settingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "export_settings.txt");
            File.WriteAllText(settingsFile, dialog.FolderName);
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

    [RelayCommand]
    private async Task ImportExcelFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "İçe aktarılacak Excel dosyalarını seçin",
            Filter = "Excel Dosyaları (*.xlsx)|*.xlsx",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true || dialog.FileNames.Length == 0)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"{dialog.FileNames.Length} dosya içe aktarılacak. Devam etmek istiyor musunuz?",
            "İçe Aktarma Onayı");

        if (!confirmed)
            return;

        await ExecuteImportAsync(async (progress, ct) =>
            await _excelImportService.ImportFilesAsync(dialog.FileNames, "merge", progress, ct));
    }

    [RelayCommand]
    private async Task ImportExcelFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Excel dosyalarının bulunduğu klasörü seçin"
        };

        if (dialog.ShowDialog() != true)
            return;

        var xlsxCount = Directory.GetFiles(dialog.FolderName, "*.xlsx", SearchOption.TopDirectoryOnly)
            .Count(f => !Path.GetFileName(f).StartsWith("~$"));

        if (xlsxCount == 0)
        {
            await _dialogService.ShowMessageAsync("Seçilen klasörde Excel dosyası bulunamadı.", "Uyarı");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Klasörde {xlsxCount} Excel dosyası bulundu. Tümünü içe aktarmak istiyor musunuz?",
            "İçe Aktarma Onayı");

        if (!confirmed)
            return;

        await ExecuteImportAsync(async (progress, ct) =>
            await _excelImportService.ImportFolderAsync(dialog.FolderName, "merge", progress, ct));
    }

    private async Task ExecuteImportAsync(
        Func<Action<int, int>, CancellationToken, Task<Core.DTOs.ExcelImportResultDto>> importFunc)
    {
        IsImporting = true;
        ImportProgress = 0;
        ImportStatusText = "İçe aktarma başlatılıyor...";

        try
        {
            var result = await importFunc((current, total) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ImportProgress = total > 0 ? (int)(current * 100.0 / total) : 0;
                    ImportStatusText = $"İşleniyor: {current}/{total}";
                });
            }, CancellationToken.None);

            ImportProgress = 100;
            ImportStatusText = "Tamamlandı!";

            var summary = $"İçe aktarma tamamlandı!\n\n" +
                          $"Başarılı dosya: {result.SuccessfulFiles}\n" +
                          $"Başarısız dosya: {result.FailedFiles}\n" +
                          $"Oluşturulan müşteri: {result.CustomersCreated}\n" +
                          $"Oluşturulan araç: {result.VehiclesCreated}\n" +
                          $"Oluşturulan işlem: {result.ServiceRecordsCreated}\n" +
                          $"Oluşturulan ödeme: {result.PaymentsCreated}\n" +
                          $"Oluşturulan teknisyen: {result.TechniciansCreated}";

            if (result.VehiclesSkipped > 0)
                summary += $"\nAtlanan araç: {result.VehiclesSkipped}";
            if (result.VehiclesMerged > 0)
                summary += $"\nBirleştirilen araç: {result.VehiclesMerged}";

            if (result.Errors.Count > 0)
                summary += $"\n\nHatalar:\n" + string.Join("\n", result.Errors.Take(10));

            await _dialogService.ShowMessageAsync(summary, "İçe Aktarma Sonucu");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"İçe aktarma sırasında hata oluştu:\n{ex.Message}",
                "Hata");
        }
        finally
        {
            IsImporting = false;
            ImportStatusText = string.Empty;
            ImportProgress = 0;
        }
    }
}
