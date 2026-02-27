using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class DailyExpensesViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportingService _reportingService;
    private readonly IDialogService _dialogService;
    private readonly IExcelExportService _excelExportService;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private ObservableCollection<DailyExpense> _expenses = new();
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _netEarnings;
    [ObservableProperty] private bool _isBusy;

    public DailyExpensesViewModel(IUnitOfWork unitOfWork, IReportingService reportingService, IDialogService dialogService, IExcelExportService excelExportService)
    {
        _unitOfWork = unitOfWork;
        _reportingService = reportingService;
        _dialogService = dialogService;
        _excelExportService = excelExportService;
    }

    public async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    async partial void OnSelectedDateChanged(DateTime value)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var expenses = await _unitOfWork.DailyExpenses.GetByDateAsync(SelectedDate);
            Expenses = new ObservableCollection<DailyExpense>(expenses);

            var summary = await _reportingService.GetDailySummaryAsync(SelectedDate);
            TotalRevenue = summary.TotalRevenue;
            TotalExpenses = summary.TotalExpenses;
            NetEarnings = summary.NetIncome;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);

    [RelayCommand]
    private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

    [RelayCommand]
    private void GoToToday() => SelectedDate = DateTime.Today;

    [RelayCommand]
    private async Task AddExpense()
    {
        var dialog = new Views.Dialogs.AddExpenseDialog();
        var vm = new ViewModels.Dialogs.AddExpenseDialogViewModel(_unitOfWork);
        await vm.InitializeAsync();
        dialog.DataContext = vm;
        if (dialog.ShowDialog() == true && vm.Result != null)
        {
            vm.Result.ExpenseDate = SelectedDate;
            await _unitOfWork.DailyExpenses.AddAsync(vm.Result);
            await _unitOfWork.SaveChangesAsync();
            await LoadDataAsync();
            _ = _excelExportService.AutoExportReportsAsync(SelectedDate);
        }
    }

    [RelayCommand]
    private async Task DeleteExpense(DailyExpense expense)
    {
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Bu gider kaydını silmek istediğinize emin misiniz?");
        if (confirmed)
        {
            var expenseDate = expense.ExpenseDate;
            await _unitOfWork.DailyExpenses.DeleteAsync(expense.Id);
            await _unitOfWork.SaveChangesAsync();
            await LoadDataAsync();
            _ = _excelExportService.AutoExportReportsAsync(expenseDate);
        }
    }
}
