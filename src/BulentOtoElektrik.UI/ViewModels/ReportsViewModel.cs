using System.Collections.ObjectModel;
using System.IO;
using BulentOtoElektrik.Core.DTOs;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
namespace BulentOtoElektrik.UI.ViewModels;

public partial class ReportsViewModel : ObservableObject
{
    private readonly IReportingService _reportingService;
    private readonly IDialogService _dialogService;
    private readonly IExcelExportService _excelExportService;

    [ObservableProperty] private DateTime _startDate;
    [ObservableProperty] private DateTime _endDate;
    [ObservableProperty] private decimal _totalRevenue;
    [ObservableProperty] private decimal _totalExpenses;
    [ObservableProperty] private decimal _netEarnings;
    [ObservableProperty] private ObservableCollection<DailyBreakdownDto> _dailyBreakdown = new();
    [ObservableProperty] private ObservableCollection<TechnicianReportDto> _technicianReport = new();
    [ObservableProperty] private ObservableCollection<ExpenseBreakdownDto> _expenseBreakdown = new();
    [ObservableProperty] private ISeries[] _pieChartSeries = [];
    [ObservableProperty] private bool _isBusy;

    public ReportsViewModel(
        IReportingService reportingService,
        IDialogService dialogService,
        IExcelExportService excelExportService)
    {
        _reportingService = reportingService;
        _dialogService = dialogService;
        _excelExportService = excelExportService;

        // Default: current month
        var today = DateTime.Today;
        _startDate = new DateTime(today.Year, today.Month, 1);
        _endDate = _startDate.AddMonths(1).AddDays(-1);
    }

    public async Task InitializeAsync()
    {
        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task LoadReportAsync()
    {
        IsBusy = true;
        try
        {
            var periodReport = await _reportingService.GetPeriodReportAsync(StartDate, EndDate);
            TotalRevenue = periodReport.TotalRevenue;
            TotalExpenses = periodReport.TotalExpenses;
            NetEarnings = periodReport.NetEarnings;
            DailyBreakdown = new ObservableCollection<DailyBreakdownDto>(periodReport.DailyBreakdown);

            var techReport = await _reportingService.GetTechnicianReportAsync(StartDate, EndDate);
            TechnicianReport = new ObservableCollection<TechnicianReportDto>(techReport);

            var expenseBreakdown = await _reportingService.GetExpenseBreakdownAsync(StartDate, EndDate);
            ExpenseBreakdown = new ObservableCollection<ExpenseBreakdownDto>(expenseBreakdown);

            PieChartSeries = expenseBreakdown.Select(e => new PieSeries<decimal>
            {
                Name = e.CategoryName,
                Values = new[] { e.TotalAmount },
            }).ToArray();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Rapor yüklenirken hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task QuickFilter(string filter)
    {
        var today = DateTime.Today;

        switch (filter)
        {
            case "Today":
                StartDate = today;
                EndDate = today;
                break;
            case "ThisWeek":
                var dayOfWeek = (int)today.DayOfWeek;
                var monday = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
                StartDate = monday;
                EndDate = monday.AddDays(6);
                break;
            case "ThisMonth":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = StartDate.AddMonths(1).AddDays(-1);
                break;
            case "LastMonth":
                var firstOfLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                StartDate = firstOfLastMonth;
                EndDate = firstOfLastMonth.AddMonths(1).AddDays(-1);
                break;
            case "ThisYear":
                StartDate = new DateTime(today.Year, 1, 1);
                EndDate = new DateTime(today.Year, 12, 31);
                break;
        }

        await LoadReportAsync();
    }

    [RelayCommand]
    private async Task ExportReport()
    {
        try
        {
            var exportFolder = _excelExportService.GetExportFolder();
            Directory.CreateDirectory(exportFolder);

            var fileName = $"Rapor_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.xlsx";
            var filePath = Path.Combine(exportFolder, fileName);

            IsBusy = true;
            await _excelExportService.ExportReportAsync(StartDate, EndDate, filePath);
            await _dialogService.ShowMessageAsync(
                $"Rapor başarıyla dışa aktarıldı.\n{filePath}", "Başarılı");
        }
        catch (Exception ex)
        {
            await _dialogService.ShowMessageAsync(
                $"Dışa aktarma sırasında hata oluştu: {ex.Message}", "Hata");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
