using Dapper;
using KoofrFolderNotifier.sql;

namespace KoofrFolderNotifier
{
    internal sealed class RecipientRepository
    {
        private readonly Database _database;

        public RecipientRepository(Database database)
        {
            ArgumentNullException.ThrowIfNull(database);
            _database = database;
        }

        public IReadOnlyList<RecipientDto> GetForFolder(int folderId)
        {
            using var db = _database.CreateConnection();
            return db.Query<RecipientDto>(
                """
                SELECT r.id, r.fullname, r.email, r.phone, r.created_at AS CreatedAt
                FROM Recipients AS r
                INNER JOIN FolderRecipients AS fr ON fr.recipient_id = r.id
                WHERE fr.folder_id = @FolderId
                ORDER BY r.id;
                """,
                new { FolderId = folderId }).ToList();
        }
    }
}
