using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.Interfaces;

namespace DatabaseManager.Services
{
    public class PostgresManager : IDatabaseManager
    {
        private string? _connectionString;
        private NpgsqlConnection? _connection;
        private NpgsqlDataAdapter? _lastAdapter;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _connectionString = connectionString;
                _connection = new NpgsqlConnection(connectionString);
                await _connection.OpenAsync();
                return _connection.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de se connecter à PostgreSQL.\nDétail de l'erreur : " + ex.Message);
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
                
                _connection = new NpgsqlConnection(_connectionString);
            }

            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
        }

        private async Task<NpgsqlConnection> GetDatabaseConnectionAsync(string databaseName)
        {
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("Veuillez d'abord initialiser la connexion via ConnectAsync.");

            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            if (!string.IsNullOrWhiteSpace(databaseName))
            {
                builder.Database = databaseName;
            }
            
            var newConnection = new NpgsqlConnection(builder.ConnectionString);
            await newConnection.OpenAsync();
            return newConnection;
        }

        public async Task<List<string>> GetDatabasesAsync()
        {
            await EnsureConnectionAsync();
            var databases = new List<string>();
            using var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate = false;", _connection);
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
            using var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteDatabaseAsync(string databaseName)
        {
            await EnsureConnectionAsync();
            using var command = new NpgsqlCommand($"DROP DATABASE \"{databaseName}\";", _connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<string>> GetTablesAsync(string databaseName)
        {
            var tables = new List<string>();
            using var dbConnection = await GetDatabaseConnectionAsync(databaseName);
            using var command = new NpgsqlCommand("SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname NOT IN ('information_schema', 'pg_catalog', 'pg_toast');", dbConnection);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
            return tables;
        }

        public async Task CreateTableAsync(string databaseName, string tableName, Dictionary<string, string> columns)
        {
            using var dbConnection = await GetDatabaseConnectionAsync(databaseName);
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE \"{tableName}\" (");
            
            var cols = new List<string>();
            foreach (var col in columns)
            {
                cols.Add($"\"{col.Key}\" {col.Value}");
            }
            sb.Append(string.Join(", ", cols));
            sb.Append(");");

            using var command = new NpgsqlCommand(sb.ToString(), dbConnection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task DropTableAsync(string databaseName, string tableName)
        {
            using var dbConnection = await GetDatabaseConnectionAsync(databaseName);
            using var command = new NpgsqlCommand($"DROP TABLE \"{tableName}\";", dbConnection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<object> ExecuteQueryAsync(string databaseName, string query)
        {
            var dbConnection = await GetDatabaseConnectionAsync(databaseName);
            var command = new NpgsqlCommand(query, dbConnection);
            var adapter = new NpgsqlDataAdapter(command);
            
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

            var builder = new NpgsqlCommandBuilder(_lastAdapter);
            await Task.Run(() => _lastAdapter.Update(dataTable));
        }
    }
}
