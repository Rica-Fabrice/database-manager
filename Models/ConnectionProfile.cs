using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace DatabaseManager.Models
{
    public enum DatabaseType
    {
        MySQL,
        PostgreSQL,
        MongoDB
    }

    public partial class ConnectionProfile : ObservableObject
    {
        [ObservableProperty]
        private Guid _id = Guid.NewGuid();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private DatabaseType _databaseType;

        partial void OnDatabaseTypeChanged(DatabaseType value)
        {
            switch (value)
            {
                case DatabaseType.MySQL:
                    Host = "localhost";
                    Port = 3306;
                    Username = "root";
                    if (string.IsNullOrEmpty(Name)) Name = "Local MySQL";
                    break;
                case DatabaseType.PostgreSQL:
                    Host = "localhost";
                    Port = 5432;
                    Username = "postgres";
                    if (string.IsNullOrEmpty(Name)) Name = "Local PostgreSQL";
                    break;
                case DatabaseType.MongoDB:
                    Host = "localhost";
                    Port = 27017;
                    Username = string.Empty;
                    Password = string.Empty;
                    if (string.IsNullOrEmpty(Name)) Name = "Local MongoDB";
                    break;
            }
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private string _host = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private int _port;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private string _username = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private string _password = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectionString))]
        private string? _defaultDatabase;

        public string ConnectionString
        {
            get
            {
                switch (DatabaseType)
                {
                    case DatabaseType.MySQL:
                        var mySb = new System.Text.StringBuilder($"Server={Host};Port={Port};User ID={Username};");
                        if (!string.IsNullOrEmpty(Password)) mySb.Append($"Password={Password};");
                        if (!string.IsNullOrWhiteSpace(DefaultDatabase)) mySb.Append($"Database={DefaultDatabase};");
                        return mySb.ToString();
                    
                    case DatabaseType.PostgreSQL:
                        var pgSb = new System.Text.StringBuilder($"Host={Host};Port={Port};Username={Username};");
                        if (!string.IsNullOrEmpty(Password)) pgSb.Append($"Password={Password};");
                        if (!string.IsNullOrWhiteSpace(DefaultDatabase)) pgSb.Append($"Database={DefaultDatabase};");
                        return pgSb.ToString();
                    
                    case DatabaseType.MongoDB:
                        if (string.IsNullOrWhiteSpace(Username) && string.IsNullOrWhiteSpace(Password))
                        {
                            return $"mongodb://{Host}:{Port}/";
                        }
                        return $"mongodb://{Username}:{Password}@{Host}:{Port}/";
                        
                    default:
                        return string.Empty;
                }
            }
        }
    }
}
