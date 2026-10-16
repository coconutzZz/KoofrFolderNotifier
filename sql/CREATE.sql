
PRAGMA foreign_keys = ON;

CREATE TABLE Recipients (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    fullname    TEXT NOT NULL,
    email       TEXT NOT NULL,
    phone       TEXT,
    created_at  TEXT NOT NULL DEFAULT (datetime('now')),
    UNIQUE (email)
);

CREATE TABLE FolderStructure (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    name        TEXT NOT NULL,
    fullpath    TEXT NOT NULL UNIQUE,
    is_active   INTEGER NOT NULL DEFAULT 1
                CHECK (is_active IN (0, 1)),
    created_at  TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE FolderRecipients (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    folder_id    INTEGER NOT NULL,
    recipient_id INTEGER NOT NULL,

    FOREIGN KEY (folder_id)
        REFERENCES FolderStructure(id) ON DELETE CASCADE,

    FOREIGN KEY (recipient_id)
        REFERENCES Recipients(id) ON DELETE CASCADE,

    UNIQUE (folder_id, recipient_id)
);

-- Updated ScannedFiles table
CREATE TABLE ScannedFiles (
    id            INTEGER PRIMARY KEY AUTOINCREMENT,
    file          TEXT NOT NULL,
    path          TEXT NOT NULL,
    folder_id     INTEGER NOT NULL,
    first_seen_at TEXT NOT NULL DEFAULT (datetime('now')),
    is_active     INTEGER NOT NULL DEFAULT 1
                  CHECK (is_active IN (0, 1)),

    FOREIGN KEY (folder_id)
        REFERENCES FolderStructure(id) ON DELETE CASCADE,

    UNIQUE (path)
);

-- Notification queue
CREATE TABLE NotificationQueue (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    scanned_file_id   INTEGER NOT NULL UNIQUE,

    status            TEXT NOT NULL DEFAULT 'Pending'
                      CHECK (status IN (
                          'Pending',
                          'Processing',
                          'Sent',
                          'Error'
                      )),

    attempts          INTEGER NOT NULL DEFAULT 0,
    created_at        TEXT NOT NULL DEFAULT (datetime('now')),
    processed_at      TEXT,
    next_attempt_at   TEXT,
    error_message     TEXT,

    FOREIGN KEY (scanned_file_id)
        REFERENCES ScannedFiles(id) ON DELETE RESTRICT
);

CREATE INDEX IX_NotificationQueue_Status
    ON NotificationQueue(status, next_attempt_at);
