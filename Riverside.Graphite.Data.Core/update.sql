BEGIN TRANSACTION;
-- Add new columns to the table
ALTER TABLE Settings ADD NewTabHistoryQuick INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryDownloads INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryFavorites INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabHistoryHistory INT DEFAULT 0;
ALTER TABLE Settings ADD NewTabSelectorBarVisible INT DEFAULT 1;
ALTER TABLE Settings ADD BackDrop VARCHAR(255) DEFAULT 'Mica';

-- Commit the transaction
COMMIT;