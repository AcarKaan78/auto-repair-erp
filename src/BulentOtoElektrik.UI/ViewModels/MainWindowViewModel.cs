using System.Collections.ObjectModel;
using System.Windows.Threading;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IReportingService _reportingService;
    private DispatcherTimer? _searchDebounceTimer;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _selectedNavItem = "Dashboard";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _todayRevenueText = "\u20ba0,00";

    [ObservableProperty]
    private int _todayVehicleCount;

    [ObservableProperty]
    private ObservableCollection<VehicleSearchResult> _searchResults = new();

    [ObservableProperty]
    private bool _isSearchPopupOpen;

    public SnackbarMessageQueue SnackbarMessageQueue { get; } = new(TimeSpan.FromSeconds(3));

    public MainWindowViewModel(
        INavigationService navigationService,
        IVehicleRepository vehicleRepository,
        IReportingService reportingService)
    {
        _navigationService = navigationService;
        _vehicleRepository = vehicleRepository;
        _reportingService = reportingService;

        _navigationService.CurrentPageChanged += vm => CurrentPage = vm;

        _searchDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _searchDebounceTimer.Tick += async (s, e) =>
        {
            _searchDebounceTimer.Stop();
            await PerformSearch();
        };
    }

    public async Task InitializeAsync()
    {
        _navigationService.NavigateTo("Dashboard");
        await RefreshStatusBar();
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer?.Stop();
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            IsSearchPopupOpen = false;
            return;
        }
        _searchDebounceTimer?.Start();
    }

    private async Task PerformSearch()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        try
        {
            var results = await _vehicleRepository.SearchAsync(SearchText);
            SearchResults = new ObservableCollection<VehicleSearchResult>(results);
            IsSearchPopupOpen = SearchResults.Count > 0;
        }
        catch
        {
            // Ignore search errors
        }
    }

    [RelayCommand]
    private void Navigate(string page)
    {
        SelectedNavItem = page;
        _navigationService.NavigateTo(page);
        SearchText = "";
        IsSearchPopupOpen = false;
    }

    [RelayCommand]
    private void SelectSearchResult(VehicleSearchResult result)
    {
        _navigationService.NavigateToCustomerDetail(result.CustomerId, result.VehicleId > 0 ? result.VehicleId : null);
        SelectedNavItem = "";
        SearchText = "";
        IsSearchPopupOpen = false;
    }

    [RelayCommand]
    private void FocusSearch()
    {
        // Handled via code-behind
    }

    [RelayCommand]
    private async Task RefreshCurrentPage()
    {
        if (!string.IsNullOrEmpty(SelectedNavItem))
        {
            _navigationService.NavigateTo(SelectedNavItem);
        }
        await RefreshStatusBar();
        SnackbarMessageQueue.Enqueue("Sayfa yenilendi");
    }

    public async Task RefreshStatusBar()
    {
        try
        {
            var summary = await _reportingService.GetDailySummaryAsync(DateTime.Today);
            TodayRevenueText = summary.TotalRevenue.ToString("C2", new System.Globalization.CultureInfo("tr-TR"));
            TodayVehicleCount = summary.VehicleCount;
        }
        catch
        {
            // Ignore on startup
        }
    }
}
