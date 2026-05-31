using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DatabaseManager.Interfaces
{
    public interface IDatabaseManager
    {
        Task<bool> ConnectAsync(string connectionString);
        Task DisconnectAsync();
        Task<List<string>> GetDatabasesAsync();
        Task CreateDatabaseAsync(string databaseName);
        Task DeleteDatabaseAsync(string databaseName);
        Task<List<string>> GetTablesAsync(string databaseName);
        Task CreateTableAsync(string databaseName, string tableName, Dictionary<string, string> columns);
        Task DropTableAsync(string databaseName, string tableName);
        Task<object> ExecuteQueryAsync(string databaseName, string query);
        Task SaveChangesAsync(DataTable dataTable);
    }
}
