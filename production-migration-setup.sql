-- Run this SQL script on your existing production database 
-- BEFORE deploying the version with migrations
-- This prevents the migration system from trying to recreate the MenuEntries table

CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
    MigrationId TEXT NOT NULL PRIMARY KEY, 
    ProductVersion TEXT NOT NULL
);

INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) 
VALUES ('20250803010915_InitialCreate', '9.0.7');

-- After running this script, deploy the new version of the application
-- The unique index migration will be applied automatically