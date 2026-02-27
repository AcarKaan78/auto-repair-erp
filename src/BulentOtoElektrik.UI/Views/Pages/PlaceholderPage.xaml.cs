using System.Windows;
using System.Windows.Controls;

namespace BulentOtoElektrik.UI.Views.Pages;

public partial class PlaceholderPage : UserControl
{
    public static readonly DependencyProperty PageNameProperty =
        DependencyProperty.Register(nameof(PageName), typeof(string), typeof(PlaceholderPage), new PropertyMetadata(""));

    public string PageName
    {
        get => (string)GetValue(PageNameProperty);
        set => SetValue(PageNameProperty, value);
    }

    public PlaceholderPage()
    {
        InitializeComponent();
    }
}
