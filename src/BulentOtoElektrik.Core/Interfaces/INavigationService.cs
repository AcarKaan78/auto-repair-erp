namespace BulentOtoElektrik.Core.Interfaces;

public interface INavigationService
{
    void NavigateTo(string pageName);
    void NavigateToCustomerDetail(int customerId);
    void GoBack();
    event Action<object>? CurrentPageChanged;
}
