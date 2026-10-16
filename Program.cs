using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

var connectionString = configuration.GetConnectionString("KoofrNotifier")
    ?? throw new InvalidOperationException("Connection string 'KoofrNotifier' is missing.");

using var db = new SqliteConnection(connectionString);
db.Open();

// Enable foreign keys on this connection.
db.Execute("PRAGMA foreign_keys = ON;");

Console.WriteLine($"Connected to SQLite database: {db.DataSource}");