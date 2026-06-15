# Deployment Notes - Authentication System

## Current Status (2026-06-15)

### Week 1: Database & Backend Infrastructure ✅ COMPLETE

All authentication infrastructure has been implemented and committed to main branch.

#### Database Migration

**Migration Name:** `AddAuthenticationSystem` (20260615101737)

**Tables Created:**
- `Users` - User accounts with email, password hash, reset token fields
- `UserRoles` - User-to-Role assignments (TeamAdmin, DivisionAdmin, SuperAdmin)
- `UserTeams` - Team Admin assignments (which teams they manage)
- `UserDivisions` - Division Admin assignments (which divisions they manage)
- `ArticleAttachments` - Previously defined but missing migration

**Fields Added to All Existing Entities:**
- `CreatedByUserId` (Guid, nullable) - Who created the record
- `UpdatedByUserId` (Guid, nullable) - Who last updated the record

#### Backend Components Implemented

**Domain Layer (iDiski.Domain/)**
- `Enums/Role.cs` - Role enum (TeamAdmin=1, DivisionAdmin=2, SuperAdmin=3)
- `Entities/User.cs` - User entity
- `Entities/UserRole.cs` - UserRole join entity
- `Entities/UserTeam.cs` - UserTeam join entity
- `Entities/UserDivision.cs` - UserDivision join entity
- `Entities/BaseEntity.cs` - Updated with audit fields

**Application Layer (iDiski.Application/)**
- `Common/Interfaces/IPasswordHasher.cs` - Password hashing abstraction
- `Common/Interfaces/IJwtTokenGenerator.cs` - JWT token generation
- `Common/Interfaces/ICurrentUserService.cs` - Current user context
- `Common/Interfaces/IEmailService.cs` - Email service for password reset
- `Common/Interfaces/ILeagueDbContext.cs` - Updated with User DbSets
- `Common/Exceptions/Exceptions.cs` - UnauthorizedException, ForbiddenException

**Infrastructure Layer (iDiski.Infrastructure/)**
- `Services/Argon2PasswordHasher.cs` - BCrypt password hashing
- `Services/JwtTokenGenerator.cs` - 15-minute expiry JWT tokens
- `Services/CurrentUserService.cs` - Extract current user from claims
- `Services/EmailService.cs` - SMTP email for password reset
- `Persistence/LeagueDbContext.cs` - User entity mapping and configuration
- `Migrations/AddAuthenticationSystem.cs` - Full migration file

**API Layer (iDiski.Api/)**
- `Middleware/ExceptionHandlingMiddleware.cs` - 401/403 response handling
- `iDiski.Api.csproj` - Added JWT Bearer, EF Tools packages

#### NuGet Packages Added
- `Microsoft.AspNetCore.Authentication.JwtBearer` 9.0.0
- `Microsoft.EntityFrameworkCore.Tools` 9.0.0
- `BCrypt.Net-Next` 4.0.3
- `MailKit` 4.8.0
- `Microsoft.AspNetCore.Http` 2.2.2
- `Microsoft.Extensions.Logging` 9.0.0

---

## Deployment Instructions

### Prerequisites
- PostgreSQL database accessible
- SMTP server configured (for password reset emails)

### Step 1: Apply Database Migration

```bash
# On deployment server/CI/CD pipeline:
dotnet ef database update \
  --project iDiski.Infrastructure \
  --startup-project iDiski.Api
```

This will:
- Create `Users` table with email unique index
- Create `UserRoles`, `UserTeams`, `UserDivisions` join tables with composite indexes
- Add `CreatedByUserId` / `UpdatedByUserId` columns to all existing tables

**Expected output:**
```
Build started...
Build succeeded.
Applying migration '20260615101737_AddAuthenticationSystem'.
Done.
```

### Step 2: Configure Environment Variables (Railway)

After migration, set these in Railway environment:

```
# JWT Configuration
Jwt__SecretKey=[64+ character random string from: openssl rand -base64 64]
Jwt__Issuer=https://api.idiski.com
Jwt__Audience=https://idiski.vercel.app

# Email Configuration (for password reset)
Email__From=noreply@idiski.com
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=[your-email@gmail.com]
Email__SmtpPassword=[app-specific-password]
```

### Step 3: Seed Initial Super Admin (Next Task)

After migration succeeds, we'll create an admin seeding command:

```bash
dotnet run --project iDiski.Api -- seed-admin \
  --email admin@idiski.com \
  --password [secure-password]
```

---

## Verification Checklist

After deployment:

- [ ] Migration runs without errors
- [ ] `Users` table exists in database
- [ ] `UserRoles`, `UserTeams`, `UserDivisions` tables exist
- [ ] All 13 existing tables have `CreatedByUserId`, `UpdatedByUserId` columns
- [ ] No foreign key constraint errors
- [ ] `User.Email` has unique index
- [ ] API starts without connection errors

---

## Next Steps (Week 2)

1. **CQRS Command Handlers**
   - `LoginCommand` / `LoginCommandHandler`
   - `CreateUserCommand` / `CreateUserCommandHandler`
   - `ForgotPasswordCommand` / `ForgotPasswordCommandHandler`
   - `ResetPasswordCommand` / `ResetPasswordCommandHandler`

2. **API Endpoints**
   - `AuthenticationController` - POST /api/auth/login, /forgot-password, /reset-password
   - `UsersController` - GET/POST/PUT /api/users (Super Admin only)

3. **Program.cs Configuration**
   - JWT authentication middleware
   - Authorization policies
   - Service registration (IPasswordHasher, IJwtTokenGenerator, etc.)
   - CORS configuration for Angular

4. **Angular Frontend**
   - `AuthService` with login state management
   - `auth.interceptor` - Attach JWT to requests
   - `role.guard` - Route protection by role
   - `LoginComponent`, `ForgotPasswordComponent`, `ResetPasswordComponent`
   - Update header to show logged-in user

---

## Database Connection Troubleshooting

If migration fails with authentication error:

1. **Check PostgreSQL is running**
   ```bash
   pg_isready -h [host] -p 5432
   ```

2. **Verify connection string**
   ```bash
   # appsettings.json or environment variable should be:
   Host=<host>;Port=5432;Database=idiski_db;Username=postgres;Password=<pwd>
   ```

3. **Test connection manually**
   ```bash
   psql -h [host] -U postgres -d idiski_db -c "SELECT 1;"
   ```

4. **For Railway deployment**
   - Use `DATABASE_URL` environment variable
   - Format: `postgresql://user:password@host:port/database`
   - Program.cs automatically converts this to Host= format

---

## Files Changed

**Total commits: 2**
- Commit 1: Domain entities + infrastructure services (21 files)
- Commit 2: EF Core migration (3 files)

**Verification:**
```bash
# Review all commits
git log --oneline -2
```

Both commits are on `main` branch and ready for deployment to Railway.
