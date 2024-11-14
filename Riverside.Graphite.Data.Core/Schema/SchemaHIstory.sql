

BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "Urls" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Urls" PRIMARY KEY AUTOINCREMENT,
    "last_visit_time" TEXT NULL,
    "url" TEXT NULL,
    "title" TEXT NULL,
    "visit_count" INTEGER NOT NULL,
    "typed_count" INTEGER NOT NULL,
    "hidden" INTEGER NOT NULL
);

COMMIT;

BEGIN TRANSACTION;

CREATE TABLE IF NOT EXISTS "Downloads" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Downloads" PRIMARY KEY AUTOINCREMENT,
    "guid" TEXT NULL,
    "current_path" TEXT NULL,
    "end_time" TEXT NULL,
    "start_time" INTEGER NOT NULL
);

COMMIT;
