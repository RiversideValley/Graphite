CREATE TABLE "CollectionNames" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_CollectionNames" PRIMARY KEY AUTOINCREMENT,
    "Name" TEXT NULL
);


CREATE TABLE "Urls" (
    "id" INTEGER NOT NULL CONSTRAINT "PK_Urls" PRIMARY KEY AUTOINCREMENT,
    "last_visit_time" TEXT NULL,
    "url" TEXT NULL,
    "title" TEXT NULL,
    "visit_count" INTEGER NOT NULL,
    "typed_count" INTEGER NOT NULL,
    "hidden" INTEGER NOT NULL
);


CREATE TABLE "Collections" (
    "Id" INTEGER NOT NULL CONSTRAINT "PK_Collections" PRIMARY KEY AUTOINCREMENT,
    "CreatedDate" TEXT NOT NULL,
    "HistoryItemId" INTEGER NOT NULL,
    "CollectionNameId" INTEGER NOT NULL,
    CONSTRAINT "FK_Collections_CollectionNames_CollectionNameId" FOREIGN KEY ("CollectionNameId") REFERENCES "CollectionNames" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Collections_Urls_HistoryItemId" FOREIGN KEY ("HistoryItemId") REFERENCES "Urls" ("id") ON DELETE CASCADE
);


CREATE INDEX "IX_Collections_CollectionNameId" ON "Collections" ("CollectionNameId");


CREATE INDEX "IX_Collections_HistoryItemId" ON "Collections" ("HistoryItemId");


