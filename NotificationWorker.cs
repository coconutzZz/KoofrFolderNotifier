using Dapper;
using KoofrFolderNotifier.sql;
using Serilog;

namespace KoofrFolderNotifier
{
    internal sealed class NotificationWorker
    {
        private readonly Database _database;
        private readonly RecipientRepository _recipientRepository;
        private readonly IEmailSender _emailSender;

        public NotificationWorker(
            Database database,
            RecipientRepository recipientRepository,
            IEmailSender emailSender)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentNullException.ThrowIfNull(recipientRepository);
            ArgumentNullException.ThrowIfNull(emailSender);

            _database = database;
            _recipientRepository = recipientRepository;
            _emailSender = emailSender;
        }

        public (int Sent, int Errors) ProcessPending()
        {
            using var db = _database.CreateConnection();
            var notifications = db.Query<NotificationQueueDto>(
                """
                SELECT id, scanned_file_id AS ScannedFileId
                FROM NotificationQueue
                WHERE status = 'Pending'
                ORDER BY created_at, id;
                """).ToList();

            var sent = 0;
            var errors = 0;

            foreach (var notification in notifications)
            {
                // Claim each entry before sending so concurrent workers do not process it twice.
                var claimed = db.Execute(
                    """
                    UPDATE NotificationQueue
                    SET status = 'Processing', attempts = attempts + 1,
                        error_message = NULL, next_attempt_at = NULL
                    WHERE id = @Id AND status = 'Pending';
                    """,
                    new { notification.Id });

                if (claimed == 0)
                {
                    continue;
                }

                try
                {
                    var file = db.QuerySingle<ScannedFileDto>(
                        """
                        SELECT id, file, path, folder_id AS FolderId,
                               first_seen_at AS FirstSeenAt, is_active AS IsActive,
                               FolderStructure.name AS FolderName
                        FROM ScannedFiles
                        INNER JOIN FolderStructure ON ScannedFiles.folder_id = FolderStructure.id
                        WHERE id = @ScannedFileId;
                        """,
                        new { notification.ScannedFileId });

                    var recipients = _recipientRepository.GetForFolder(file.FolderId);
                    if (recipients.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"No recipients are configured for folder {file.FolderId} ({file.Path}).");
                    }

                    var sendErrors = new List<string>();
                    foreach (var recipient in recipients)
                    {
                        try
                        {
                            _emailSender.Send(recipient, file);
                        }
                        catch (Exception exception)
                        {
                            sendErrors.Add($"{recipient.Email}: {exception.Message}");
                        }
                    }

                    if (sendErrors.Count > 0)
                    {
                        throw new InvalidOperationException(string.Join("; ", sendErrors));
                    }

                    db.Execute(
                        """
                        UPDATE NotificationQueue
                        SET status = 'Sent', processed_at = datetime('now'),
                            error_message = NULL, next_attempt_at = NULL
                        WHERE id = @Id AND status = 'Processing';
                        """,
                        new { notification.Id });

                    sent++;
                }
                catch (Exception exception)
                {
                    db.Execute(
                        """
                        UPDATE NotificationQueue
                        SET status = 'Error', processed_at = datetime('now'),
                            error_message = @ErrorMessage, next_attempt_at = NULL
                        WHERE id = @Id AND status = 'Processing';
                        """,
                        new { notification.Id, ErrorMessage = exception.Message });

                    Log.Error(exception, "Notification {NotificationId} failed.", notification.Id);
                    errors++;
                }
            }

            return (sent, errors);
        }
    }
}
