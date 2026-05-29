-- =============================================================================
-- seed-data.sql
-- Optional: Seed reference / static data after migration.
-- Run via: psql -h <azure-host> -U <user> -d <db> -f scripts/seed-data.sql
-- Or uncomment the "seed-data" job in .github/workflows/deploy.yml
-- =============================================================================

BEGIN;

-- ──────────────────────────────────────────
-- Example: Seed default labels for projects
-- ──────────────────────────────────────────
-- INSERT INTO "Labels" ("Id", "Name", "Color", "ProjectId", "CreatedAt")
-- VALUES
--   (gen_random_uuid(), 'bug',     '#ef4444', NULL, NOW()),
--   (gen_random_uuid(), 'feature', '#3b82f6', NULL, NOW()),
--   (gen_random_uuid(), 'enhancement', '#10b981', NULL, NOW()),
--   (gen_random_uuid(), 'documentation', '#f59e0b', NULL, NOW()),
--   (gen_random_uuid(), 'urgent',  '#dc2626', NULL, NOW());

-- ──────────────────────────────────────────
-- Example: Seed an admin user (password hashed)
-- ──────────────────────────────────────────
-- INSERT INTO "Users" ("Id", "Email", "Username", "PasswordHash", "DisplayName", "CreatedAt")
-- VALUES
--   (gen_random_uuid(), 'admin@example.com', 'admin',
--    '$2a$11$...', 'Administrator', NOW())
-- ON CONFLICT ("Email") DO NOTHING;

COMMIT;
