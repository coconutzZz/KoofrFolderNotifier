using Dapper;
using KoofrFolderNotifier.sql;
using Microsoft.Data.Sqlite;

namespace KoofrFolderNotifier
{
    internal sealed class FileScanner
    {
        private readonly Database _database;
        private readonly string _baseFolder;

        public FileScanner(Database database, string baseFolder)
        {
            ArgumentNullException.ThrowIfNull(database);
            ArgumentException.ThrowIfNullOrWhiteSpace(baseFolder);

            _database = database;
            _baseFolder = Path.GetFullPath(baseFolder, AppContext.BaseDirectory);
        }

        public int Scan()
        {
            if (!Directory.Exists(_baseFolder))
            {
                throw new DirectoryNotFoundException($"BaseFolder does not exist: {_baseFolder}");
            }

            using var db = _database.CreateConnection();
            var folders = db.Query<FolderStructureDto>(
                "SELECT id, fullpath FROM FolderStructure WHERE is_active = 1;")
                .Select(folder => new { folder.Id, Path = ResolveFolderPath(folder) })
                // Give nested configured folders ownership of their own documents.
                .OrderByDescending(folder => folder.Path.Length)
                .ThenBy(folder => folder.Id)
                .ToList();

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = false,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            var queuedFiles = 0;

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder.Path))
                {
                    Console.Error.WriteLine($"Skipping missing folder: {folder.Path}");
                    continue;
                }

                foreach (var filePath in Directory.EnumerateFiles(folder.Path, "*", options))
                {
                    var extension = Path.GetExtension(filePath);
                    if (!extension.Equals(".doc", StringComparison.OrdinalIgnoreCase)
                        && !extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(_baseFolder, filePath);
                    if (QueueNewFile(db, folder.Id, relativePath))
                    {
                        queuedFiles++;
                    }
                }
            }

            return queuedFiles;
        }

        private string ResolveFolderPath(FolderStructureDto folder)
        {
            if (string.IsNullOrWhiteSpace(folder.Fullpath))
            {
                throw new InvalidOperationException($"FolderStructure entry {folder.Id} has no path.");
            }

            var folderPath = Path.GetFullPath(folder.Fullpath, _baseFolder);
            var relativePath = Path.GetRelativePath(_baseFolder, folderPath);
            if (Path.IsPathRooted(relativePath)
                || relativePath == ".."
                || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"FolderStructure entry {folder.Id} is outside BaseFolder: {folder.Fullpath}");
            }

            return folderPath;
        }

        private static bool QueueNewFile(SqliteConnection db, int folderId, string relativePath)
        {
            // Reserve the writer before checking history so concurrent scans cannot queue the same path.
            using var transaction = db.BeginTransaction(deferred: false);
            var parameters = new
            {
                File = Path.GetFileName(relativePath),
                Path = relativePath,
                FolderId = folderId
            };

            // Include all historical records, even if the document was deleted or marked inactive.
            var alreadyScanned = db.ExecuteScalar<bool>(
                "SELECT EXISTS (SELECT 1 FROM ScannedFiles WHERE path = @Path);",
                parameters, transaction);

            if (alreadyScanned)
            {
                return false;
            }

            var scannedFileId = db.ExecuteScalar<long>(
                """
                INSERT INTO ScannedFiles (file, path, folder_id)
                VALUES (@File, @Path, @FolderId)
                RETURNING id;
                """,
                parameters, transaction);

            db.Execute(
                """
                INSERT INTO NotificationQueue (scanned_file_id, status)
                VALUES (@ScannedFileId, 'Pending');
                """,
                new { ScannedFileId = scannedFileId }, transaction);

            transaction.Commit();
            return true;
        }
    }
}
