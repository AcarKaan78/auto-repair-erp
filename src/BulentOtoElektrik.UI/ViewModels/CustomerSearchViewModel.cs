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
            // Load all customers (including those without vehicles)
            var customers = await _unitOfWork.Customers.GetAllAsync();
            var results = new List<VehicleSearchResult>();

            foreach (var c in customers)
            {
                var vehicles = await _unitOfWork.Vehicles.GetByCustomerIdAsync(c.Id);
                if (vehicles.Count > 0)
                {
                    foreach (var v in vehicles)
                    {
                        var serviceRecords = await _unitOfWork.ServiceRecords.GetByVehicleIdAsync(v.Id);
                        var payments = await _unitOfWork.Payments.GetByCustomerIdAsync(c.Id);
                        results.Add(new VehicleSearchResult
                        {
                            VehicleId = v.Id,
                            CustomerId = c.Id,
                            PlateNumber = v.PlateNumber,
                            CustomerName = c.FullName,
                            VehicleModel = $"{v.VehicleBrand} {v.VehicleModel}".Trim(),
                            Balance = serviceRecords.Sum(sr => sr.TotalAmount) - payments.Sum(p => p.Amount)
                        });
                    }
                }
                else
                {
                    // Show customers without vehicles too
                    results.Add(new VehicleSearchResult
                    {
                        VehicleId = 0,
                        CustomerId = c.Id,
                        PlateNumber = "-",
                        CustomerName = c.FullName,
                        VehicleModel = "Arac eklenmemis",
                        Balance = 0
                    });
                }
            }

            SearchResults = new ObservableCollection<VehicleSearchResult>(results);
            StatusMessage = SearchResults.Count == 0
                ? "Henuz kayitli musteri bulunmuyor"
                : $"{SearchResults.Count} kayit listelendi";
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
        var dialogVm = new AddCustomerDialogViewModel(_unitOfWork, _dialogService);
        var customer = await _dialogService.ShowDialogAsync<Customer>(dialogVm);
        if (customer != null)
        {
            try
            {
                _navigationService.NavigateToCustomerDetail(customer.Id);
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
            _navigationService.NavigateToCustomerDetail(result.CustomerId, result.VehicleId > 0 ? result.VehicleId : null);
        }
    }
}
