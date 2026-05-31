using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.Interfaces;

namespace DatabaseManager.Services
{
    public class MysqlManager : IDatabaseManager
    {
        private MySqlConnection? _connection;
        private string? _connectionString;
        private MySqlDataAdapter? _lastAdapter;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _connectionString = connectionString;
                _connection = new MySqlConnection(connectionString);
                await _connection.OpenAsync();
                return _connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de se connecter à MySQL.\nDétail de l'erreur : " + ex.Message);
            }
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection == null)
            {
                if (string.IsNullOrEmpty(_connectionString))
                    throw new InvalidOperationException("Veuillez d'abord initialiser la connexion via ConnectAsync.");
                
                _connection = new MySqlConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        private async Task UseDatabaseAsync(string databaseName)
        {
            await EnsureConnectionAsync();
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                using var useCmd = new MySqlCommand($"USE `{databaseName}`;", _connection);
                await useCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<string>> GetDatabasesAsync()
        {
            await EnsureConnectionAsync();
            var databases = new List<string>();
            using var command = new MySqlCommand("SHOW DATABASES;", _connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
            return databases;
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            await EnsureConnectionAsync();
            using var command = new MySqlCommand($"CREATE DATABASE `{databaseName}`;", _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDatabaseAsync(string databaseName)
        {
            await EnsureConnectionAsync();
            using var command = new MySqlCommand($"DROP DATABASE `{databaseName}`;", _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetTablesAsync(string databaseName)
        {
            await UseDatabaseAsync(databaseName);
            var tables = new List<string>();
            using var command = new MySqlCommand("SHOW TABLES;", _connection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            return tables;
        }

        public async Task CreateTableAsync(string databaseName, string tableName, Dictionary<string, string> columns)
        {
            await UseDatabaseAsync(databaseName);
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE `{tableName}` (");
            
            var cols = new List<string>();
            foreach (var col in columns)
            {
                cols.Add($"`{col.Key}` {col.Value}");
            }
            sb.Append(string.Join(", ", cols));
            sb.Append(");");

            using var command = new MySqlCommand(sb.ToString(), _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DropTableAsync(string databaseName, string tableName)
        {
            await UseDatabaseAsync(databaseName);
            using var command = new MySqlCommand($"DROP TABLE `{tableName}`;", _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteQueryAsync(string databaseName, string query)
        {
            await UseDatabaseAsync(databaseName);
            var command = new MySqlCommand(query, _connection);
            var adapter = new MySqlDataAdapter(command);
            
            var dataTable = new DataTable();
            await Task.Run(() => adapter.Fill(dataTable));
            
            // Stocker l'adapter pour la sauvegarde ultérieure
            _lastAdapter = adapter;
            
            return dataTable;
        }

        public async Task SaveChangesAsync(DataTable dataTable)
        {
            if (_lastAdapter == null)
                throw new InvalidOperationException("Aucune requête n'a été exécutée récemment pour permettre la sauvegarde.");

            var builder = new MySqlCommandBuilder(_lastAdapter);
            await Task.Run(() => _lastAdapter.Update(dataTable));
        }
    }
}
