using System.Windows.Controls;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.UI.Helpers;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<(object vm, IServiceScope scope)> _navigationStack = new();
    private object? _currentViewModel;
    private IServiceScope? _currentScope;

    public event Action<object>? CurrentPageChanged;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            CurrentPageChanged?.Invoke(value!);
        }
    }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo(string pageName)
    {
        // Create a new scope so each ViewModel gets its own DbContext
        var scope = _serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var viewModel = pageName switch
        {
            "Dashboard" => sp.GetRequiredService<ViewModels.DashboardViewModel>() as object,
            "CustomerSearch" => sp.GetRequiredService<ViewModels.CustomerSearchViewModel>(),
            "NewService" => sp.GetRequiredService<ViewModels.NewServiceViewModel>(),
            "DailyExpenses" => sp.GetRequiredService<ViewModels.DailyExpensesViewModel>(),
            "Reports" => sp.GetRequiredService<ViewModels.ReportsViewModel>(),
            "Technicians" => sp.GetRequiredService<ViewModels.TechniciansViewModel>(),
            "StokTakip" => sp.GetRequiredService<ViewModels.StokTakipViewModel>(),
            "Personnel" => sp.GetRequiredService<ViewModels.PersonnelViewModel>(),
            "Settings" => sp.GetRequiredService<ViewModels.SettingsViewModel>(),
            _ => sp.GetRequiredService<ViewModels.DashboardViewModel>()
        };

        if (_currentViewModel != null && _currentScope != null)
            _navigationStack.Push((_currentViewModel, _currentScope));

        _currentScope = scope;
        CurrentViewModel = viewModel;
        TriggerAutoExport();
    }

    public void NavigateToCustomerDetail(int customerId, int? vehicleId = null)
    {
        var scope = _serviceProvider.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<ViewModels.CustomerDetailViewModel>();
        vm.CustomerId = customerId;
        vm.PreferredVehicleId = vehicleId;

        if (_currentViewModel != null && _currentScope != null)
            _navigationStack.Push((_currentViewModel, _currentScope));

        _currentScope = scope;
        CurrentViewModel = vm;
        TriggerAutoExport();
    }

    public void GoBack()
    {
        // Dispose current scope since we're leaving this page
        _currentScope?.Dispose();
        _currentScope = null;

        if (_navigationStack.Count > 0)
        {
            var (vm, scope) = _navigationStack.Pop();
            _currentScope = scope;
            CurrentViewModel = vm;
            TriggerAutoExport();
        }
    }

    private void TriggerAutoExport()
    {
        try
        {
            var excelService = _serviceProvider.GetRequiredService<IExcelExportService>();
            _ = excelService.AutoExportAllAsync();
        }
        catch { }
    }
}
