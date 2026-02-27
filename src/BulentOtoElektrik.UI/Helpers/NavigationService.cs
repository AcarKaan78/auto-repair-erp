using System.Windows.Controls;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.UI.Helpers;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<object> _navigationStack = new();
    private object? _currentViewModel;

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
        var viewModel = pageName switch
        {
            "Dashboard" => _serviceProvider.GetRequiredService<ViewModels.DashboardViewModel>() as object,
            "CustomerSearch" => _serviceProvider.GetRequiredService<ViewModels.CustomerSearchViewModel>(),
            "NewService" => _serviceProvider.GetRequiredService<ViewModels.NewServiceViewModel>(),
            "DailyExpenses" => _serviceProvider.GetRequiredService<ViewModels.DailyExpensesViewModel>(),
            "Reports" => _serviceProvider.GetRequiredService<ViewModels.ReportsViewModel>(),
            "Technicians" => _serviceProvider.GetRequiredService<ViewModels.TechniciansViewModel>(),
            "Settings" => _serviceProvider.GetRequiredService<ViewModels.SettingsViewModel>(),
            _ => _serviceProvider.GetRequiredService<ViewModels.DashboardViewModel>()
        };

        if (_currentViewModel != null)
            _navigationStack.Push(_currentViewModel);

        CurrentViewModel = viewModel;
        TriggerAutoExport();
    }

    public void NavigateToCustomerDetail(int customerId)
    {
        var vm = _serviceProvider.GetRequiredService<ViewModels.CustomerDetailViewModel>();
        vm.CustomerId = customerId;

        if (_currentViewModel != null)
            _navigationStack.Push(_currentViewModel);

        CurrentViewModel = vm;
        TriggerAutoExport();
    }

    public void GoBack()
    {
        if (_navigationStack.Count > 0)
        {
            CurrentViewModel = _navigationStack.Pop();
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
