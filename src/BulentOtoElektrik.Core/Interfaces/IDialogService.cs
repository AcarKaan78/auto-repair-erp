namespace BulentOtoElektrik.Core.Interfaces;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string message, string title = "Onay");
    Task ShowMessageAsync(string message, string title = "Bilgi");
    Task<T?> ShowDialogAsync<T>(object viewModel) where T : class;
}
