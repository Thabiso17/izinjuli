-- Data Migration: Convert old position strings to new PlayerPosition enum values
-- Generated: 2026-05-22
-- This script updates existing player positions from old generic values to new specific positions

START TRANSACTION;

-- Update old position strings to new enum values
-- Old: Goalkeeper, Defender, Midfielder, Forward
-- New: GK, CB, CM, ST (defaulting to common positions)

-- Goalkeeper → GK
UPDATE "Players"
SET "Position" = 'GK'
WHERE "Position" = 'Goalkeeper';

-- Defender → CB (Center Back as default)
UPDATE "Players"
SET "Position" = 'CB'
WHERE "Position" = 'Defender';

-- Midfielder → CM (Central Midfielder as default)
UPDATE "Players"
SET "Position" = 'CM'
WHERE "Position" = 'Midfielder';

-- Forward → ST (Striker as default)
UPDATE "Players"
SET "Position" = 'ST'
WHERE "Position" = 'Forward';

COMMIT;

-- Verify the updates
SELECT "Position", COUNT(*) as player_count
FROM "Players"
GROUP BY "Position"
ORDER BY "Position";
