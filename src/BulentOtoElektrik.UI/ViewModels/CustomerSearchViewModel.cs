using System.Collections.ObjectModel;
using System.Windows.Threading;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using BulentOtoElektrik.UI.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class CustomerSearchViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private DispatcherTimer? _searchDebounceTimer;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private ObservableCollection<VehicleSearchResult> _searchResults = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Musteri aramak icin yukaridaki arama kutusunu kullanin";

    public CustomerSearchViewModel(
        IUnitOfWork unitOfWork,
        INavigationService navigationService,
        IDialogService dialogService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _dialogService = dialogService;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await PerformSearchAsync();
        };
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer?.Stop();
        if (string.IsNullOrWhiteSpace(value))
        {
            _ = LoadRecentCustomersAsync();
            return;
        }
        _searchDebounceTimer?.Start();
    }

    public async Task LoadRecentCustomersAsync()
    {
        IsLoading = true;
        try
        {
            var results = await _unitOfWork.Vehicles.SearchAsync("");
            SearchResults = new ObservableCollection<VehicleSearchResult>(results);
            StatusMessage = SearchResults.Count == 0
                ? "Henuz kayitli musteri bulunmuyor"
                : $"{SearchResults.Count} musteri listelendi";
        }
        catch
        {
            StatusMessage = "Musteriler yuklenirken hata olustu";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsLoading = true;
        try
        {
            var results = await _unitOfWork.Vehicles.SearchAsync(SearchText);
            SearchResults = new ObservableCollection<VehicleSearchResult>(results);
            StatusMessage = SearchResults.Count == 0
                ? $"'{SearchText}' icin sonuc bulunamadi"
                : $"{SearchResults.Count} sonuc bulundu";
        }
        catch
        {
            StatusMessage = "Arama sirasinda hata olustu";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await PerformSearchAsync();
    }

    [RelayCommand]
    private async Task AddCustomerAsync()
    {
        var dialogVm = new AddCustomerDialogViewModel();
        var customer = await _dialogService.ShowDialogAsync<Customer>(dialogVm);
        if (customer != null)
        {
            try
            {
                await _unitOfWork.Customers.AddAsync(customer);
                await _unitOfWork.SaveChangesAsync();
                await _dialogService.ShowMessageAsync("Musteri basariyla eklendi.", "Basarili");
                await LoadRecentCustomersAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageAsync($"Musteri eklenirken hata olustu: {ex.Message}", "Hata");
            }
        }
    }

    [RelayCommand]
    private void ViewCustomer(VehicleSearchResult? result)
    {
        if (result != null)
        {
            _navigationService.NavigateToCustomerDetail(result.CustomerId);
        }
    }
}
