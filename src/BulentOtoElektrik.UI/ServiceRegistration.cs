using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.UI;

public static class ServiceRegistration
{
    public static void RegisterServices(IServiceCollection services)
    {
        // Navigation and Dialog Services
        services.AddSingleton<Helpers.NavigationService>();
        services.AddSingleton<Core.Interfaces.INavigationService>(sp => sp.GetRequiredService<Helpers.NavigationService>());
        services.AddSingleton<Helpers.DialogService>();
        services.AddSingleton<Core.Interfaces.IDialogService>(sp => sp.GetRequiredService<Helpers.DialogService>());

        // ViewModels
        services.AddTransient<ViewModels.MainWindowViewModel>();
        services.AddTransient<ViewModels.DashboardViewModel>();
        services.AddTransient<ViewModels.CustomerSearchViewModel>();
        services.AddTransient<ViewModels.CustomerDetailViewModel>();
        services.AddTransient<ViewModels.NewServiceViewModel>();
        services.AddTransient<ViewModels.DailyExpensesViewModel>();
        services.AddTransient<ViewModels.ReportsViewModel>();
        services.AddTransient<ViewModels.TechniciansViewModel>();
        services.AddTransient<ViewModels.SettingsViewModel>();

        // Dialog ViewModels
        services.AddTransient<ViewModels.Dialogs.AddCustomerDialogViewModel>();
        services.AddTransient<ViewModels.Dialogs.AddVehicleDialogViewModel>();
        services.AddTransient<ViewModels.Dialogs.AddServiceDialogViewModel>();
        services.AddTransient<ViewModels.Dialogs.PaymentDialogViewModel>();
        services.AddTransient<ViewModels.Dialogs.AddExpenseDialogViewModel>();

        // Main Window
        services.AddSingleton<Views.MainWindow>();
    }
}
