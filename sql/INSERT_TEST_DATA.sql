-- Run CREATE.sql first. BaseFolder must be D:\temp.
-- Safe to rerun: existing recipients, folders, and mappings are preserved.
PRAGMA foreign_keys = ON;

BEGIN TRANSACTION;

INSERT INTO Recipients (fullname, email)
VALUES ('Janez Novak', 'uros.jures@gmail.com')
ON CONFLICT (email) DO NOTHING;

INSERT INTO FolderStructure (name, fullpath, is_active)
VALUES
    ('1.članki', 'D:\temp\1.članki', 1),
    ('2. za lektoriranje', 'D:\temp\2. za lektoriranje', 1)
ON CONFLICT (fullpath) DO NOTHING;

-- Resolve generated IDs so this also works in a database with existing data.
INSERT INTO FolderRecipients (folder_id, recipient_id)
SELECT f.id, r.id
FROM FolderStructure AS f
CROSS JOIN Recipients AS r
WHERE f.fullpath IN ('D:\temp\1.članki', 'D:\temp\2. za lektoriranje')
  AND r.email = 'uros.jures@gmail.com'
ON CONFLICT (folder_id, recipient_id) DO NOTHING;

COMMIT;
