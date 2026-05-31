using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DatabaseManager.ViewModels;

namespace DatabaseManager;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private TextBox? _passwordTextBox;
    private bool _isPasswordVisible = false;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox && DataContext is MainViewModel viewModel)
        {
            if (viewModel.SelectedConnection != null)
            {
                viewModel.SelectedConnection.Password = passwordBox.Password;
            }
        }
    }

    private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible && DataContext is MainViewModel viewModel)
        {
            if (viewModel.SelectedConnection != null)
            {
                viewModel.SelectedConnection.Password = PasswordTextBox.Text;
            }
        }
    }

    private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            // Switch back to PasswordBox
            PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;

            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            _isPasswordVisible = false;
        }
        else
        {
            // Switch to TextBox
            PasswordTextBox.TextChanged -= PasswordTextBox_TextChanged;
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordTextBox.TextChanged += PasswordTextBox_TextChanged;

            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
            _isPasswordVisible = true;
        }
    }
}