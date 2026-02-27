using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BulentOtoElektrik.UI.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportingService _reportingService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private decimal _todayRevenue;

    [ObservableProperty]
    private decimal _todayExpenses;

    [ObservableProperty]
    private decimal _netEarnings;

    [ObservableProperty]
    private int _vehicleCount;

    [ObservableProperty]
    private double _revenueChangePercent;

    [ObservableProperty]
    private double _expenseChangePercent;

    [ObservableProperty]
    private double _netChangePercent;

    [ObservableProperty]
    private ObservableCollection<ServiceRecord> _recentServices = new();

    [ObservableProperty]
    private ObservableCollection<Customer> _topDebtors = new();

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private LiveChartsCore.SkiaSharpView.Axis[] _xAxes = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();

    [ObservableProperty]
    private LiveChartsCore.SkiaSharpView.Axis[] _yAxes = Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();

    [ObservableProperty]
    private bool _isBusy;

    public DashboardViewModel(
        IUnitOfWork unitOfWork,
        IReportingService reportingService,
        INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _reportingService = reportingService;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            // 1. Load daily summary
            var summary = await _reportingService.GetDailySummaryAsync(DateTime.Today);
            TodayRevenue = summary.TotalRevenue;
            TodayExpenses = summary.TotalExpenses;
            NetEarnings = summary.NetIncome;
            VehicleCount = summary.VehicleCount;
            RevenueChangePercent = summary.RevenueChangePercent;
            ExpenseChangePercent = summary.ExpenseChangePercent;
            NetChangePercent = summary.NetChangePercent;

            // 2. Load recent services
            var recentServices = await _unitOfWork.ServiceRecords.GetRecentAsync(10);
            RecentServices = new ObservableCollection<ServiceRecord>(recentServices);

            // 3. Load top debtors
            var topDebtors = await _unitOfWork.Customers.GetTopDebtorsAsync(10);
            TopDebtors = new ObservableCollection<Customer>(topDebtors);

            // 4. Build chart data for current month
            var firstDayOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
            var periodReport = await _reportingService.GetPeriodReportAsync(firstDayOfMonth, lastDayOfMonth);

            var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);
            var dailyRevenues = new decimal[daysInMonth];
            var dailyExpenses = new decimal[daysInMonth];
            var dayLabels = new string[daysInMonth];

            for (int i = 0; i < daysInMonth; i++)
            {
                dayLabels[i] = (i + 1).ToString();
            }

            foreach (var day in periodReport.DailyBreakdown)
            {
                var dayIndex = day.Date.Day - 1;
                if (dayIndex >= 0 && dayIndex < daysInMonth)
                {
                    dailyRevenues[dayIndex] = day.Revenue;
                    dailyExpenses[dayIndex] = day.Expenses;
                }
            }

            ChartSeries = new ISeries[]
            {
                new ColumnSeries<decimal>
                {
                    Name = "Gelir",
                    Values = dailyRevenues,
                    Fill = new SolidColorPaint(SKColors.Green.WithAlpha(180)),
                },
                new LineSeries<decimal>
                {
                    Name = "Gider",
                    Values = dailyExpenses,
                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 2 },
                    Fill = null,
                    GeometrySize = 8,
                }
            };

            XAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labels = dayLabels,
                    LabelsRotation = 0,
                }
            };

            YAxes = new LiveChartsCore.SkiaSharpView.Axis[]
            {
                new LiveChartsCore.SkiaSharpView.Axis
                {
                    Labeler = value => value.ToString("C0", new System.Globalization.CultureInfo("tr-TR")),
                }
            };
        }
        catch
        {
            // Silently handle initialization errors on dashboard
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void NavigateToCustomer(Customer? customer)
    {
        if (customer != null)
        {
            _navigationService.NavigateToCustomerDetail(customer.Id);
        }
    }

    [RelayCommand]
    private void NavigateToServiceCustomer(ServiceRecord? serviceRecord)
    {
        if (serviceRecord?.Vehicle?.Customer != null)
        {
            _navigationService.NavigateToCustomerDetail(serviceRecord.Vehicle.Customer.Id);
        }
        else if (serviceRecord?.Vehicle != null)
        {
            _navigationService.NavigateToCustomerDetail(serviceRecord.Vehicle.CustomerId);
        }
    }

}
