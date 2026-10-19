using KoofrFolderNotifier;
using Microsoft.Extensions.Configuration;
using Serilog;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

const string logPathSetting = "Serilog:WriteTo:File:Args:path";
var logFilePath = configuration[logPathSetting]
    ?? throw new InvalidOperationException($"Setting '{logPathSetting}' must be configured.");

// Resolve relative log paths beside the application, including when run by a scheduler.
configuration[logPathSetting] = Path.GetFullPath(
    Environment.ExpandEnvironmentVariables(logFilePath), AppContext.BaseDirectory);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    Log.Information("Koofr Folder Notifier started in {Environment} environment.", environment);

    var connectionString = configuration.GetConnectionString("KoofrNotifier")
        ?? throw new InvalidOperationException("Connection string 'KoofrNotifier' is missing.");

    var database = new Database(connectionString);

    var baseFolder = configuration["BaseFolder"];
    if (string.IsNullOrWhiteSpace(baseFolder))
    {
        throw new InvalidOperationException("Setting 'BaseFolder' must be configured.");
    }

    var scanner = new FileScanner(database, baseFolder);
    var queuedFiles = scanner.Scan();

    if (queuedFiles == 0)
    {
        Log.Information("No new documents found.");
        return;
    }

    Log.Information("Queued notifications for {QueuedFiles} new document(s).", queuedFiles);

    var recipientRepository = new RecipientRepository(database);
    var fromAddress = configuration["Gmail:FromAddress"];
    if (string.IsNullOrWhiteSpace(fromAddress))
    {
        throw new InvalidOperationException("Setting 'Gmail:FromAddress' must be configured.");
    }

    var fromPassword = Environment.GetEnvironmentVariable("GMAIL_FROM_PASSWORD");
    if (string.IsNullOrWhiteSpace(fromPassword))
    {
        throw new InvalidOperationException("Environment variable 'GMAIL_FROM_PASSWORD' must be set.");
    }

    var emailSender = new GmailEmailSender(fromAddress, fromPassword);
    var notificationWorker = new NotificationWorker(database, recipientRepository, emailSender);
    var (sent, errors) = notificationWorker.ProcessPending();

    Log.Information("Notification processing completed: {Sent} sent, {Errors} failed.", sent, errors);
}
catch (Exception exception)
{
    Log.Fatal(exception, "Koofr Folder Notifier terminated unexpectedly.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
