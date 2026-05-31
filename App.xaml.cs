using System.Configuration;
using System.Data;
using System.Windows;

namespace DatabaseManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        try
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur au démarrage: {ex.Message}\n\n{ex.StackTrace}", "Erreur de démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

