using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DatabaseManager.Interfaces;
using DatabaseManager.Models;
using DatabaseManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DatabaseManager.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ConnectionProfile> _savedConnections = new();

        [ObservableProperty]
        private ConnectionProfile? _selectedConnection;

        [ObservableProperty]
        private bool _isConnected;

        partial void OnIsConnectedChanged(bool value)
        {
            ExecuteQueryCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
            CreateDatabaseCommand.NotifyCanExecuteChanged();
            DeleteDatabaseCommand.NotifyCanExecuteChanged();
            CreateTableCommand.NotifyCanExecuteChanged();
            DeleteTableCommand.NotifyCanExecuteChanged();
            RefreshHierarchyCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private ObservableCollection<DatabaseNode> _databaseNodes = new();

        [ObservableProperty]
        private TableNode? _selectedTable;

        [ObservableProperty]
        private string _queryText = string.Empty;

        [ObservableProperty]
        private DataTable? _queryResult;

        partial void OnQueryResultChanged(DataTable? value)
        {
            SaveChangesCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private string? _selectedDatabaseForQuery;

        partial void OnSelectedDatabaseForQueryChanged(string? value)
        {
            ExecuteQueryCommand.NotifyCanExecuteChanged();
        }

        [ObservableProperty]
        private ObservableCollection<string> _availableDatabases = new();

        private IDatabaseManager? _currentManager;

        private const string ConnectionsFilePath = "connections.json";

        // Expose l'énumération pour le binding de la ComboBox
        public Array DatabaseTypes => Enum.GetValues(typeof(DatabaseType));

        public MainViewModel()
        {
            try
            {
                LoadConnections();
            }
            catch (Exception ex)
            {
                // En cas d'erreur de chargement, continuer avec une liste vide
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des connexions: {ex.Message}");
            }
            
            if (SavedConnections.Count == 0)
            {
                // Ajout d'un profil par défaut pour la démo
                var defaultProfile = new ConnectionProfile
                {
                    Name = "Local MySQL",
                    Host = "localhost",
                    Port = 3306,
                    Username = "root",
                    DatabaseType = DatabaseType.MySQL
                };
                SavedConnections.Add(defaultProfile);
                SelectedConnection = defaultProfile;
            }
            else
            {
                SelectedConnection = SavedConnections[0];
            }
        }

        [RelayCommand]
        private void AddConnection()
        {
            var newProfile = new ConnectionProfile
            {
                Name = "Nouvelle Connexion MySQL",
                DatabaseType = DatabaseType.MySQL
            };
            SavedConnections.Add(newProfile);
            SelectedConnection = newProfile;
            SaveConnections();
        }

        [RelayCommand(CanExecute = nameof(CanDeleteConnection))]
        private void DeleteConnection()
        {
            if (SelectedConnection != null)
            {
                SavedConnections.Remove(SelectedConnection);
                SelectedConnection = null;
                SaveConnections();
            }
        }

        private bool CanDeleteConnection() => SelectedConnection != null;

        // Méthode générée automatiquement par CommunityToolkit lors du changement de SelectedConnection
        partial void OnSelectedConnectionChanged(ConnectionProfile? value)
        {
            DeleteConnectionCommand.NotifyCanExecuteChanged();
            TestConnectionCommand.NotifyCanExecuteChanged();
            ConnectCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            if (SelectedConnection == null) return;

            try
            {
                _currentManager = DatabaseManagerFactory.GetManager(SelectedConnection.DatabaseType);
                bool success = await _currentManager.ConnectAsync(SelectedConnection.ConnectionString);

                if (success)
                {
                    IsConnected = true;
                    await LoadDatabaseHierarchyAsync();
                    MessageBox.Show("Connexion réussie !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Échec de la connexion. Veuillez vérifier vos paramètres.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Échec : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private async Task DisconnectAsync()
        {
            if (_currentManager != null)
            {
                await _currentManager.DisconnectAsync();
                _currentManager = null;
            }
            IsConnected = false;
            DatabaseNodes.Clear();
        }

        private bool CanConnect() => SelectedConnection != null && !IsConnected;
        private bool CanDisconnect() => IsConnected;

        [RelayCommand(CanExecute = nameof(CanExecuteQuery))]
        private async Task ExecuteQueryAsync()
        {
            if (_currentManager == null || string.IsNullOrWhiteSpace(SelectedDatabaseForQuery) || string.IsNullOrWhiteSpace(QueryText))
            {
                MessageBox.Show("Veuillez sélectionner une base de données et saisir une requête.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var result = await _currentManager.ExecuteQueryAsync(SelectedDatabaseForQuery, QueryText);

                // Vérifier si la requête modifie le schéma et rafraîchir la hiérarchie
                var normalizedQuery = QueryText.Trim().ToUpper();
                bool shouldRefreshHierarchy = normalizedQuery.StartsWith("CREATE TABLE") ||
                                              normalizedQuery.StartsWith("DROP TABLE") ||
                                              normalizedQuery.StartsWith("CREATE DATABASE") ||
                                              normalizedQuery.StartsWith("DROP DATABASE") ||
                                              normalizedQuery.StartsWith("ALTER TABLE");

                if (result is DataTable dataTable)
                {
                    QueryResult = dataTable;
                }
                else if (result is string jsonString)
                {
                    // Pour MongoDB, afficher le JSON formaté
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(jsonString);
                        var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });

                        // Créer un DataTable simple pour afficher le JSON
                        var jsonTable = new DataTable();
                        jsonTable.Columns.Add("Résultat JSON");
                        jsonTable.Rows.Add(formattedJson);
                        QueryResult = jsonTable;
                    }
                    catch
                    {
                        var jsonTable = new DataTable();
                        jsonTable.Columns.Add("Résultat");
                        jsonTable.Rows.Add(jsonString);
                        QueryResult = jsonTable;
                    }
                }
                else
                {
                    QueryResult = null;
                    MessageBox.Show("Requête exécutée avec succès (aucun résultat à afficher).", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Rafraîchir la hiérarchie si la requête a modifié le schéma
                if (shouldRefreshHierarchy)
                {
                    await LoadDatabaseHierarchyAsync();
                }
            }
            catch (Exception ex)
            {
                var errorTable = new DataTable();
                errorTable.Columns.Add("Erreur d'exécution");
                errorTable.Rows.Add(ex.Message);
                QueryResult = errorTable;
            }
        }

        private bool CanExecuteQuery() => IsConnected && !string.IsNullOrWhiteSpace(SelectedDatabaseForQuery);

        [RelayCommand]
        private async Task SelectTableAsync(TableNode? table)
        {
            if (table == null || _currentManager == null) return;

            try
            {
                // Trouver la base de données parente
                var parentDatabase = DatabaseNodes.FirstOrDefault(db => db.Tables.Contains(table));
                if (parentDatabase == null) return;

                // Sélectionner la base de données et la table
                SelectedDatabaseForQuery = parentDatabase.Name;
                SelectedTable = table;

                // Générer la requête SELECT selon le type de base de données
                string query;
                if (SelectedConnection?.DatabaseType == DatabaseType.MongoDB)
                {
                    query = $"db.{table.Name}.find().limit(100)";
                }
                else
                {
                    query = $"SELECT * FROM {table.Name} LIMIT 100;";
                }

                QueryText = query;

                // Exécuter automatiquement la requête
                await ExecuteQueryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sélection de la table : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanManageDatabase))]
        private async Task CreateDatabaseAsync()
        {
            if (_currentManager == null) return;

            string dbName = PromptForInput("Nom de la nouvelle base de données :", "Créer une base de données");
            if (string.IsNullOrWhiteSpace(dbName)) return;

            try
            {
                await _currentManager.CreateDatabaseAsync(dbName);
                await LoadDatabaseHierarchyAsync();
                MessageBox.Show($"Base de données '{dbName}' créée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand(CanExecute = nameof(CanManageDatabase))]
        private async Task DeleteDatabaseAsync()
        {
            if (_currentManager == null || string.IsNullOrWhiteSpace(SelectedDatabaseForQuery)) return;

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer la base de données '{SelectedDatabaseForQuery}' ?", 
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _currentManager.DeleteDatabaseAsync(SelectedDatabaseForQuery);
                    await LoadDatabaseHierarchyAsync();
                    MessageBox.Show($"Base de données '{SelectedDatabaseForQuery}' supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanManageDatabase))]
        private async Task CreateTableAsync()
        {
            if (_currentManager == null || string.IsNullOrWhiteSpace(SelectedDatabaseForQuery)) return;

            string tableName = PromptForInput("Nom de la nouvelle table :", "Créer une table");
            if (string.IsNullOrWhiteSpace(tableName)) return;

            try
            {
                var columns = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "id", "INT PRIMARY KEY" },
                    { "name", "VARCHAR(255)" }
                };
                
                await _currentManager.CreateTableAsync(SelectedDatabaseForQuery, tableName, columns);
                await LoadDatabaseHierarchyAsync();
                MessageBox.Show($"Table '{tableName}' créée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string? PromptForInput(string prompt, string title)
        {
            var inputWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(label, 0);

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };
            Grid.SetRow(textBox, 1);

            var okButton = new Button { Content = "OK", Width = 80, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(okButton, 2);

            var cancelButton = new Button { Content = "Annuler", Width = 80, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 0) };
            Grid.SetRow(cancelButton, 2);

            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(okButton);
            grid.Children.Add(cancelButton);

            inputWindow.Content = grid;

            string? result = null;
            okButton.Click += (s, e) => { result = textBox.Text; inputWindow.Close(); };
            cancelButton.Click += (s, e) => inputWindow.Close();
            textBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) { result = textBox.Text; inputWindow.Close(); } };

            inputWindow.ShowDialog();
            return result;
        }

        [RelayCommand(CanExecute = nameof(CanManageDatabase))]
        private async Task DeleteTableAsync()
        {
            if (_currentManager == null || string.IsNullOrWhiteSpace(SelectedDatabaseForQuery) || SelectedTable == null) return;

            var result = MessageBox.Show($"Êtes-vous sûr de vouloir supprimer la table '{SelectedTable.Name}' ?", 
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _currentManager.DropTableAsync(SelectedDatabaseForQuery, SelectedTable.Name);
                    await LoadDatabaseHierarchyAsync();
                    MessageBox.Show($"Table '{SelectedTable.Name}' supprimée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la suppression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanManageDatabase() => IsConnected && _currentManager != null;

        [RelayCommand(CanExecute = nameof(CanManageDatabase))]
        private async Task RefreshHierarchyAsync()
        {
            await LoadDatabaseHierarchyAsync();
        }

        [RelayCommand(CanExecute = nameof(CanSaveChanges))]
        private async Task SaveChangesAsync()
        {
            if (_currentManager == null || QueryResult == null) return;

            try
            {
                await _currentManager.SaveChangesAsync(QueryResult);
                MessageBox.Show("Modifications sauvegardées avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveChanges() => IsConnected && _currentManager != null && QueryResult != null;

        [RelayCommand(CanExecute = nameof(CanTestConnection))]
        private async Task TestConnectionAsync()
        {
            if (SelectedConnection == null) return;

            try
            {
                var manager = DatabaseManagerFactory.GetManager(SelectedConnection.DatabaseType);
                bool success = await manager.ConnectAsync(SelectedConnection.ConnectionString);

                if (success)
                {
                    MessageBox.Show("Connexion réussie !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Échec de la connexion. Veuillez vérifier vos paramètres et l'état du serveur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                await manager.DisconnectAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du test de connexion :\n\n{ex.Message}", "Erreur Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanTestConnection() => SelectedConnection != null;

        private async Task LoadDatabaseHierarchyAsync()
        {
            if (_currentManager == null) return;

            try
            {
                DatabaseNodes.Clear();
                AvailableDatabases.Clear();

                // Récupérer la liste des bases de données depuis le serveur
                var databases = await _currentManager.GetDatabasesAsync();
                
                foreach (var dbName in databases)
                {
                    AvailableDatabases.Add(dbName);
                    
                    var tableNodes = new ObservableCollection<TableNode>();
                    
                    try
                    {
                        // Récupérer les tables pour chaque base
                        var tables = await _currentManager.GetTablesAsync(dbName);
                        
                        foreach (var tableName in tables)
                        {
                            tableNodes.Add(new TableNode { Name = tableName });
                        }
                    }
                    catch (Exception tableEx)
                    {
                        // Diagnostic temporaire pour voir l'erreur
                        MessageBox.Show($"Erreur pour '{dbName}':\n{tableEx.GetType().Name}: {tableEx.Message}", "Diagnostic", MessageBoxButton.OK, MessageBoxImage.Warning);
                        System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des tables pour '{dbName}': {tableEx.Message}");
                    }
                    
                    DatabaseNodes.Add(new DatabaseNode
                    {
                        Name = dbName,
                        Tables = tableNodes
                    });
                }

                // Sélectionner la première base de données par défaut ou celle spécifiée dans le profil
                if (!string.IsNullOrWhiteSpace(SelectedConnection?.DefaultDatabase))
                {
                    SelectedDatabaseForQuery = SelectedConnection.DefaultDatabase;
                }
                else if (AvailableDatabases.Count > 0)
                {
                    SelectedDatabaseForQuery = AvailableDatabases[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement de la hiérarchie : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // En cas d'erreur, utiliser des données de démonstration
                var demoDatabase = new DatabaseNode
                {
                    Name = SelectedConnection?.DefaultDatabase ?? "Base de données",
                    Tables = new ObservableCollection<TableNode>
                    {
                        new TableNode { Name = "Table 1" },
                        new TableNode { Name = "Table 2" },
                        new TableNode { Name = "Table 3" }
                    }
                };
                DatabaseNodes.Add(demoDatabase);
                AvailableDatabases.Add(demoDatabase.Name);
                SelectedDatabaseForQuery = demoDatabase.Name;
            }
        }

        private void SaveConnections()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(SavedConnections, options);
                File.WriteAllText(ConnectionsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadConnections()
        {
            try
            {
                if (File.Exists(ConnectionsFilePath))
                {
                    string json = File.ReadAllText(ConnectionsFilePath);
                    var connections = JsonSerializer.Deserialize<ObservableCollection<ConnectionProfile>>(json);
                    if (connections != null)
                    {
                        SavedConnections = connections;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
