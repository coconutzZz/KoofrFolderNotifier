namespace KoofrFolderNotifier.sql
{    public class RecipientDto
    {
        public int Id { get; set; }
        public string Fullname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class FolderStructureDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Fullpath { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class FolderRecipientDto
    {
        public int Id { get; set; }
        public int FolderId { get; set; }
        public int RecipientId { get; set; }
    }

    public class ScannedFileDto
    {
        public int Id { get; set; }
        public string File { get; set; } = string.Empty;
        public int FolderId { get; set; }
        public string FirstSeenAt { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class NotificationQueueDto
    {
        public int Id { get; set; }
        public int ScannedFileId { get; set; }
        public string Status { get; set; } = "Pending";
        public int Attempts { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? ProcessedAt { get; set; }
        public string? NextAttemptAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

}
