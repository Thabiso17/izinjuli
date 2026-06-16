// This is a standalone helper to generate the admin user password hash
// Run this in the Application project to get the correct BCrypt hash
//
// Usage in .NET project:
// dotnet script GenerateAdminHash.cs
//
// Or copy-paste in a test:
// using BCrypt.Net;
// string password = "Admin@Password123";
// string hash = BCrypt.HashPassword(password, 12);
// Console.WriteLine(hash);

using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "Admin@Password123";
        string hash = BCrypt.HashPassword(password, 12);

        Console.WriteLine("Admin User SQL Insert Script");
        Console.WriteLine("=============================\n");
        Console.WriteLine($"Email: admin@example.com");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"BCrypt Hash: {hash}\n");

        Console.WriteLine("Copy and run this SQL in your database:\n");
        Console.WriteLine($@"
WITH new_user AS (
  INSERT INTO ""Users"" (
    ""Email"",
    ""PasswordHash"",
    ""FirstName"",
    ""LastName"",
    ""IsActive"",
    ""CreatedAt""
  ) VALUES (
    'admin@example.com',
    '{hash}',
    'Admin',
    'User',
    true,
    NOW()
  )
  RETURNING ""Id""
)
INSERT INTO ""UserRoles"" (
  ""UserId"",
  ""Role"",
  ""AssignedAt""
)
SELECT ""Id"", 3, NOW() FROM new_user;
");
    }
}
