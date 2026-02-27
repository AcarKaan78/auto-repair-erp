using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class StokTakipViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDialogService _dialogService;

    // Collections
    [ObservableProperty] private ObservableCollection<StockItem> _stockItems = new();
    [ObservableProperty] private ObservableCollection<StockItem> _filteredStockItems = new();

    // Add form fields
    [ObservableProperty] private string _newMaterialName = "";
    [ObservableProperty] private string _newStockQuantity = "";
    [ObservableProperty] private string _newUnitPrice = "";
    [ObservableProperty] private bool _isAddFormVisible;

    // Edit state
    [ObservableProperty] private StockItem? _editingItem;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private string _editMaterialName = "";
    [ObservableProperty] private string _editUnitPrice = "";
    [ObservableProperty] private string _restockQuantity = "";

    // Search
    [ObservableProperty] private string _searchText = "";

    // Summary
    [ObservableProperty] private int _totalItemsCount;
    [ObservableProperty] private decimal _totalStockValue;
    [ObservableProperty] private int _lowStockWarningCount;

    // Busy
    [ObservableProperty] private bool _isBusy;

    public StokTakipViewModel(IUnitOfWork unitOfWork, IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync()
    {
        await LoadStockItemsAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private async Task LoadStockItemsAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _unitOfWork.StockItems.GetActiveAsync();
            StockItems = new ObservableCollection<StockItem>(items);
            ApplyFilter();
            UpdateSummary();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredStockItems = new ObservableCollection<StockItem>(StockItems);
        }
        else
        {
            var lower = SearchText.Trim().ToLower();
            FilteredStockItems = new ObservableCollection<StockItem>(
                StockItems.Where(s => s.MaterialName.ToLower().Contains(lower)));
        }
    }

    private void UpdateSummary()
    {
        TotalItemsCount = StockItems.Count;
        TotalStockValue = StockItems.Sum(s => s.RemainingQuantity * s.UnitPrice);
        LowStockWarningCount = StockItems.Count(s => s.RemainingQuantity < 5);
    }

    [RelayCommand]
    private void ToggleAddForm()
    {
        IsAddFormVisible = !IsAddFormVisible;
        if (!IsAddFormVisible)
        {
            NewMaterialName = "";
            NewStockQuantity = "";
            NewUnitPrice = "";
        }
    }

    [RelayCommand]
    private async Task AddStockItem()
    {
        if (string.IsNullOrWhiteSpace(NewMaterialName))
        {
            await _dialogService.ShowMessageAsync("Malzeme adi bos olamaz.", "Uyari");
            return;
        }
        if (!int.TryParse(NewStockQuantity, out var qty) || qty <= 0)
        {
            await _dialogService.ShowMessageAsync("Gecerli bir adet giriniz.", "Uyari");
            return;
        }
        if (!decimal.TryParse(NewUnitPrice, out var price))
            price = 0;

        IsBusy = true;
        try
        {
            var item = new StockItem
            {
                MaterialName = NewMaterialName.Trim(),
                StockQuantity = qty,
                RemainingQuantity = qty,
                UnitPrice = price,
                IsActive = true
            };
            await _unitOfWork.StockItems.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            StockItems.Add(item);
            ApplyFilter();
            UpdateSummary();

            NewMaterialName = "";
            NewStockQuantity = "";
            NewUnitPrice = "";
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Malzeme eklenirken hata: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteStockItem(StockItem? item)
    {
        if (item == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"'{item.MaterialName}' malzemesini silmek istediginize emin misiniz?", "Malzeme Sil");
        if (!confirmed) return;

        IsBusy = true;
        try
        {
            item.IsActive = false;
            await _unitOfWork.StockItems.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
            StockItems.Remove(item);
            ApplyFilter();
            UpdateSummary();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Silme hatasi: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void StartEdit(StockItem? item)
    {
        if (item == null) return;
        EditingItem = item;
        EditMaterialName = item.MaterialName;
        EditUnitPrice = item.UnitPrice.ToString();
        RestockQuantity = "";
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingItem = null;
    }

    [RelayCommand]
    private async Task SaveEdit()
    {
        if (EditingItem == null) return;

        if (string.IsNullOrWhiteSpace(EditMaterialName))
        {
            await _dialogService.ShowMessageAsync("Malzeme adi bos olamaz.", "Uyari");
            return;
        }

        IsBusy = true;
        try
        {
            EditingItem.MaterialName = EditMaterialName.Trim();
            if (decimal.TryParse(EditUnitPrice, out var price))
                EditingItem.UnitPrice = price;

            if (!string.IsNullOrWhiteSpace(RestockQuantity)
                && int.TryParse(RestockQuantity, out var restock) && restock > 0)
            {
                EditingItem.StockQuantity += restock;
                EditingItem.RemainingQuantity += restock;
            }

            await _unitOfWork.StockItems.UpdateAsync(EditingItem);
            await _unitOfWork.SaveChangesAsync();

            IsEditing = false;
            EditingItem = null;
            await LoadStockItemsAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync($"Guncelleme hatasi: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
