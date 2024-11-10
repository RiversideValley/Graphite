

BEGIN TRANSACTION;

-- Add new columns to the table
ALTER TABLE Settings ADD NewTabHistoryQuick INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryDownloads INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryFavorites INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryHistory INT DEFAULT 0;

-- Commit the transaction
COMMIT;