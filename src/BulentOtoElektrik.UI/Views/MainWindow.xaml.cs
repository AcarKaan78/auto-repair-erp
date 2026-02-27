using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BulentOtoElektrik.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BulentOtoElektrik.UI.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private bool _suppressNavChange;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            await _viewModel.InitializeAsync();
        };

        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.SelectedNavItem))
            {
                SyncNavSelection(_viewModel.SelectedNavItem);
            }
        };
    }

    private void SyncNavSelection(string pageName)
    {
        _suppressNavChange = true;
        for (int i = 0; i < NavListBox.Items.Count; i++)
        {
            if (NavListBox.Items[i] is ListBoxItem item && item.Tag?.ToString() == pageName)
            {
                NavListBox.SelectedIndex = i;
                break;
            }
        }
        _suppressNavChange = false;
    }

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressNavChange || _viewModel == null) return;
        if (NavListBox.SelectedItem is ListBoxItem item && item.Tag is string page)
        {
            _viewModel.NavigateCommand.Execute(page);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.NavigateCommand.Execute("NewService");
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.SearchText = "";
            _viewModel.IsSearchPopupOpen = false;
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _viewModel.RefreshCurrentPageCommand.Execute(null);
            e.Handled = true;
        }
    }
}
