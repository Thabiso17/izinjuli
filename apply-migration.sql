-- Migration: AddPreferredFootToPlayer
-- Generated: 2026-05-22
-- This script adds the PreferredFoot column to the Players table

START TRANSACTION;

-- Check if migration is already applied, if not apply it
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260522150134_AddPreferredFootToPlayer') THEN
        -- Add PreferredFoot column
        ALTER TABLE "Players" ADD "PreferredFoot" integer NOT NULL DEFAULT 0;

        -- Record migration in history
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260522150134_AddPreferredFootToPlayer', '9.0.0');
    END IF;
END $EF$;

COMMIT;

-- Verify the column was added
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'Players' AND column_name = 'PreferredFoot';
