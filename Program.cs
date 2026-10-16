using KoofrFolderNotifier;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{environment}.json", optional: true)
    .Build();

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

Console.WriteLine($"Queued notifications for {queuedFiles} new document(s).");

var recipientRepository = new RecipientRepository(database);
var emailSender = new MockEmailSender();
var notificationWorker = new NotificationWorker(database, recipientRepository, emailSender);
var (sent, errors) = notificationWorker.ProcessPending();

Console.WriteLine($"Processed notifications (mock email): {sent} sent, {errors} errors.");
