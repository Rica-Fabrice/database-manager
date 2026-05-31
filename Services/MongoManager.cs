using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DatabaseManager.Interfaces;

namespace DatabaseManager.Services
{
    public class MongoManager : IDatabaseManager
    {
        private MongoClient? _client;

        public async Task<bool> ConnectAsync(string connectionString)
        {
            try
            {
                _client = new MongoClient(connectionString);
                
                // Exécute une commande très légère (lister les bases de données)
                // pour forcer MongoDB à valider immédiatement la connexion réseau
                var cursor = await _client.ListDatabaseNamesAsync();
                await cursor.MoveNextAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Impossible de se connecter à MongoDB sur ce port/hôte.\nDétail de l'erreur : " + ex.Message);
            }
        }

        public Task DisconnectAsync()
        {
            // Dans le driver MongoDB, le client maintient automatiquement 
            // un pool de connexions et ne possède pas de méthode "Close" explicite.
            // On libère simplement l'instance.
            _client = null;
            return Task.CompletedTask;
        }

        private MongoClient EnsureClient()
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Veuillez d'abord initialiser la connexion via ConnectAsync.");
            }
            return _client;
        }

        public async Task<List<string>> GetDatabasesAsync()
        {
            var client = EnsureClient();
            var databases = new List<string>();
            var cursor = await client.ListDatabaseNamesAsync();
            while (await cursor.MoveNextAsync())
            {
                databases.AddRange(cursor.Current);
            }
            return databases;
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            var client = EnsureClient();
            var database = client.GetDatabase(databaseName);
            // Crée une collection par défaut pour forcer la création de la base, 
            // car MongoDB ne crée physiquement la base que lorsqu'elle contient des données/collections.
            await database.CreateCollectionAsync("init_collection");
        }

        public async Task DeleteDatabaseAsync(string databaseName)
        {
            var client = EnsureClient();
            await client.DropDatabaseAsync(databaseName);
        }

        public async Task<List<string>> GetTablesAsync(string databaseName)
        {
            var client = EnsureClient();
            var database = client.GetDatabase(databaseName);
            var tables = new List<string>();
            var cursor = await database.ListCollectionNamesAsync();
            while (await cursor.MoveNextAsync())
            {
                tables.AddRange(cursor.Current);
            }
            return tables;
        }

        public async Task CreateTableAsync(string databaseName, string tableName, Dictionary<string, string> columns)
        {
            var client = EnsureClient();
            var database = client.GetDatabase(databaseName);
            // Les colonnes sont ignorées car MongoDB est "Schema-Less" (NoSQL orienté document)
            await database.CreateCollectionAsync(tableName);
        }

        public async Task DropTableAsync(string databaseName, string tableName)
        {
            var client = EnsureClient();
            var database = client.GetDatabase(databaseName);
            await database.DropCollectionAsync(tableName);
        }

        public async Task<object> ExecuteQueryAsync(string databaseName, string query)
        {
            var client = EnsureClient();
            var database = client.GetDatabase(databaseName);
            
            // On considère que query est une commande MongoDB valide formatée en JSON,
            // par exemple : { ping: 1 } ou { buildInfo: 1 }
            var commandDocument = BsonDocument.Parse(query);
            
            var result = await database.RunCommandAsync<BsonDocument>(commandDocument);
            
            // On renvoie le résultat formaté en JSON (String)
            return result.ToJson();
        }

        public Task SaveChangesAsync(DataTable dataTable)
        {
            throw new NotSupportedException("L'édition directe des données n'est pas supportée pour MongoDB. Utilisez les requêtes MongoDB pour modifier les documents.");
        }
    }
}
