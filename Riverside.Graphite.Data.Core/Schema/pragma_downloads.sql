CREATE TABLE "Downloads" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Downloads" PRIMARY KEY AUTOINCREMENT,
    "guid" TEXT NULL,
    "current_path" TEXT NULL,
    "end_time" TEXT NULL,
    "start_time" INTEGER NOT NULL
);


