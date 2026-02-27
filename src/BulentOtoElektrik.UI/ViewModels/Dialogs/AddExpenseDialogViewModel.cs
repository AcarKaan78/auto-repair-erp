using System.Collections.ObjectModel;
using BulentOtoElektrik.Core.Entities;
using BulentOtoElektrik.Core.Enums;
using BulentOtoElektrik.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BulentOtoElektrik.UI.ViewModels.Dialogs;

public partial class AddExpenseDialogViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty] private ObservableCollection<ExpenseCategory> _categories = new();
    [ObservableProperty] private ExpenseCategory? _selectedCategory;
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private CurrencyType _selectedCurrency = CurrencyType.TL;

    public DailyExpense? Result { get; private set; }
    public Array CurrencyTypes => Enum.GetValues(typeof(CurrencyType));

    public AddExpenseDialogViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task InitializeAsync()
    {
        var cats = await _unitOfWork.ExpenseCategories.GetActiveAsync();
        Categories = new ObservableCollection<ExpenseCategory>(cats);
        if (Categories.Count > 0) SelectedCategory = Categories[0];
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedCategory == null || Amount <= 0) return;
        Result = new DailyExpense
        {
            CategoryId = SelectedCategory.Id,
            Description = Description,
            Amount = Amount,
            Currency = SelectedCurrency
        };
    }
}
