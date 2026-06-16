-- Insert Super Admin user for testing
-- Email: admin@example.com
-- Password: Admin@Password123

-- STEP 1: Insert the user and assign SuperAdmin role
WITH new_user AS (
  INSERT INTO "Users" (
    "Id",
    "Email",
    "PasswordHash",
    "FirstName",
    "LastName",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
  ) VALUES (
    gen_random_uuid(),
    'admin@example.com',
    '$2a$12$R9h7cIPz0gi.URNNX3kh2OPST9/PgBjGH8KDH.iq8dVK2nW8nEHLy', -- BCrypt hash of "Admin@Password123"
    'Admin',
    'User',
    true,
    NOW(),
    NOW()
  )
  RETURNING "Id"
)
INSERT INTO "UserRoles" (
  "Id",
  "UserId",
  "Role",
  "AssignedAt",
  "CreatedAt",
  "UpdatedAt"
)
SELECT gen_random_uuid(), "Id", 3, NOW(), NOW(), NOW()
FROM new_user;

-- Verify the user was created
SELECT "Id", "Email", "FirstName", "LastName", "IsActive", "CreatedAt"
FROM "Users"
WHERE "Email" = 'admin@example.com';

-- Verify the role was assigned
SELECT ur."Id", ur."UserId", ur."Role", u."Email"
FROM "UserRoles" ur
JOIN "Users" u ON ur."UserId" = u."Id"
WHERE u."Email" = 'admin@example.com';
