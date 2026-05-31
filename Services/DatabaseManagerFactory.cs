using DatabaseManager.Interfaces;
using DatabaseManager.Models;
using System;

namespace DatabaseManager.Services
{
    public static class DatabaseManagerFactory
    {
        public static IDatabaseManager GetManager(DatabaseType dbType)
        {
            return dbType switch
            {
                DatabaseType.MySQL => new MysqlManager(),
                DatabaseType.PostgreSQL => new PostgresManager(),
                DatabaseType.MongoDB => new MongoManager(),
                _ => throw new NotSupportedException($"Le type de base de données '{dbType}' n'est pas supporté.")
            };
        }
    }
}
