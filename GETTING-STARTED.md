# iDiski League - Getting Started Guide

This guide will help you set up and run the iDiski Football League Management System.

---

## 📋 Prerequisites

### Required Software
- **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 18+** & **npm** - [Download](https://nodejs.org/)
- **PostgreSQL 15+** - [Download](https://www.postgresql.org/download/)
- **Git** - [Download](https://git-scm.com/)

### Optional Tools
- **Visual Studio 2022** or **VS Code**
- **Postman** or **Swagger UI** (for API testing)
- **pgAdmin** (for database management)

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Database Setup

1. **Start PostgreSQL** service
2. **Create database**:
   ```sql
   CREATE DATABASE idiski_db;
   ```

3. **Update connection string** in `iDiski.Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=idiski_db;Username=postgres;Password=YOUR_PASSWORD"
     }
   }
   ```

### Step 2: Backend Setup

```bash
# Navigate to solution directory
cd C:\Users\CP378669\source\repos\diski

# Restore NuGet packages
dotnet restore

# Run EF migrations
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api

# Start the API
cd iDiski.Api
dotnet run
```

**Backend will start on**: `http://localhost:5207`

### Step 3: Frontend Setup

```bash
# Open new terminal
cd C:\Users\CP378669\source\repos\diski\iDiski-Client

# Install dependencies (first time only)
npm install

# Start development server
npm start
# OR
ng serve
```

**Frontend will start on**: `http://localhost:4200`

### Step 4: Seed Sample Data

**Option A: Via API Endpoint** (Recommended)
1. Ensure backend is running
2. Open browser and navigate to:
   ```
   http://localhost:5207/api/seed
   ```
3. You should see: `{ "message": "Database seeded successfully!" }`

**Option B: Via Swagger UI**
1. Navigate to `http://localhost:5207/swagger`
2. Find the `GET /api/seed` endpoint
3. Click "Try it out" → "Execute"

**What gets seeded:**
- ✅ 3 Divisions (U15 Boys, U15 Girls, Senior Men)
- ✅ 6 Teams (Orlando Pirates, Kaizer Chiefs, Sundowns, etc.)
- ✅ 108 Players (18 per team with random stats)
- ✅ 15+ Matches (mix of completed and upcoming)

---

## 🏗️ Project Structure

```
diski/
├── iDiski.Api/              # Web API (Controllers, Program.cs)
├── iDiski.Application/      # CQRS (Commands, Queries, DTOs)
├── iDiski.Domain/           # Entities, Enums, Domain Services
├── iDiski.Infrastructure/   # EF Core, Persistence, Migrations
│   └── Seed/                # Sample data seeder
└── iDiski-Client/           # Angular 21 frontend
    └── src/
        └── app/
            ├── features/    # Page components
            │   ├── teams/
            │   ├── players/
            │   ├── matches/
            │   ├── standings/
            │   ├── admin/
            │   └── home/
            ├── core/
            │   ├── models/  # TypeScript DTOs
            │   └── services/ # API services
            └── layouts/     # Header, Footer
```

---

## 🎯 First Steps After Setup

### 1. Explore the Admin Pages

Navigate to: `http://localhost:4200/admin`

**Try these workflows:**

#### A. Create a Division
1. Go to **Admin → Divisions**
2. Click "Add Division"
3. Fill in:
   - Name: "Premier League U17 Boys"
   - Short Code: "U17B"
   - Season: 2026
   - Age Group: "U17"
   - Gender: "Male"
4. Click "Create"

#### B. Add a Team
1. Go to **Admin → Teams**
2. Click "Add Team"
3. Fill in:
   - Name: "Test FC"
   - Short Code: "TEST"
   - Founded: 2020
   - City: "Johannesburg"
   - Division: Select from dropdown
   - Colors: Pick team colors
   - Logo URL: Paste image URL (or leave blank)
4. Click "Create"

#### C. Register a Player
1. Go to **Admin → Players**
2. Click "Add Player"
3. Fill in:
   - First Name: "John"
   - Last Name: "Doe"
   - Team: Select team
   - Jersey Number: 10
   - Position: Midfielder
   - Date of Birth: Pick date
4. Click "Create"

#### D. Schedule a Match
1. Go to **Admin → Matches**
2. Click "Schedule Match"
3. Fill in:
   - Home Team: Select team
   - Away Team: Select different team
   - Date: Pick future date
   - Matchweek: 1
   - Season: Current year
   - Venue: Stadium name
4. Click "Create"

#### E. Record Match Result
1. Find a scheduled match
2. Click "Update Score"
3. Enter scores
4. Change status to "Completed"
5. (Optional) Record match events:
   - Click "Add Event"
   - Select player, event type (Goal/Card), minute
6. Click "Save"

### 2. Browse Public Pages

Navigate to these URLs:

- **Home**: `http://localhost:4200/`
- **Teams**: `http://localhost:4200/teams`
- **Matches**: `http://localhost:4200/matches`
- **Standings**: `http://localhost:4200/standings`

Click through to explore:
- Team detail pages with squad lists
- Player profiles with stats
- Match details with event timelines
- League table with form guide

---

## 🔧 Development Tips

### Backend (C# / .NET)

#### Add New Entity
```bash
# 1. Create entity class in iDiski.Domain/Entities/
# 2. Add DbSet to LeagueDbContext
# 3. Configure in OnModelCreating
# 4. Create migration
dotnet ef migrations add AddNewEntity --project iDiski.Infrastructure --startup-project iDiski.Api
# 5. Apply migration
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api
```

#### Add New Endpoint
1. Create Command/Query in `iDiski.Application/`
2. Add Validator (if needed)
3. Add Handler
4. Create Controller in `iDiski.Api/Controllers/`
5. Test via Swagger

#### Hot Reload
```bash
dotnet watch run --project iDiski.Api
# Changes to C# files auto-reload
```

### Frontend (Angular)

#### Generate Component
```bash
cd iDiski-Client
ng generate component features/my-feature
```

#### Generate Service
```bash
ng generate service core/services/my-service
```

#### Build for Production
```bash
ng build --configuration production
# Output: dist/iDiski-Client/
```

#### Run Tests
```bash
ng test
# Opens Vitest UI
```

---

## 📝 Common Tasks

### Reset Database
```bash
# Drop and recreate database
dotnet ef database drop --project iDiski.Infrastructure --startup-project iDiski.Api --force
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api

# Re-seed data
# Navigate to http://localhost:5207/api/seed
```

### Update API URL (Frontend)
Edit `iDiski-Client/src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5207/api', // Change port if needed
};
```

### View Swagger Documentation
Navigate to: `http://localhost:5207/swagger`

### View Database
Use pgAdmin or:
```bash
psql -U postgres -d idiski_db
# Then run SQL queries
```

---

## 🐛 Troubleshooting

### Backend Won't Start

**Error: "A connection was not established"**
- ✅ Check PostgreSQL is running
- ✅ Verify connection string in `appsettings.json`
- ✅ Ensure database exists: `CREATE DATABASE idiski_db;`

**Error: "Pending model changes"**
```bash
dotnet ef migrations add InitialCreate --project iDiski.Infrastructure --startup-project iDiski.Api
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api
```

**Port 5207 Already in Use**
- Edit `iDiski.Api/Properties/launchSettings.json`
- Change `applicationUrl` to different port

### Frontend Won't Start

**Error: "ng: command not found"**
```bash
npm install -g @angular/cli
```

**Error: "Port 4200 is already in use"**
```bash
ng serve --port 4201
```

**API Requests Failing (CORS Error)**
- ✅ Ensure backend is running
- ✅ Check `apiBaseUrl` in `environment.ts`
- ✅ Verify CORS is configured in `Program.cs`

### Seed Data Issues

**Error: "Database already seeded"**
- This is normal if data already exists
- To re-seed, drop database and run migrations again

**Error: "Duplicate key value violates unique constraint"**
- Tables already have data
- Drop tables or reset database

---

## 🎨 Customization

### Change Color Scheme
Edit `iDiski-Client/src/styles.scss`:
```scss
// Override Bootstrap variables
$primary: #your-color;
$secondary: #your-color;
```

### Add Your League Logo
1. Place image in `iDiski-Client/src/assets/`
2. Update `HeaderComponent` to use your logo

### Modify Team Colors
1. Go to Admin → Teams
2. Edit team
3. Use color pickers for Primary/Secondary colors

---

## 📊 Sample Data Overview

After seeding, you'll have:

### Divisions (3)
- **U15 Boys** (Premier League U15 Boys)
- **U15 Girls** (Premier League U15 Girls)
- **Senior Men** (Senior Men's League)

### Teams (6)
All in Senior Men's division:
- Orlando Pirates (ORL) - Black/White
- Kaizer Chiefs (CHI) - Gold/Black
- Mamelodi Sundowns (SUN) - Yellow/Blue
- SuperSport United (SSU) - Blue/White
- AmaZulu FC (AMA) - Green/White
- Cape Town City (CTC) - Sky Blue/White

### Players (108)
- 18 players per team
- Realistic South African names
- Random stats (goals, assists, cards)
- Distributed positions (2 GK, 6 DEF, 6 MID, 4 FWD)

### Matches (15+)
- Round-robin fixtures
- Mix of completed and scheduled
- Random scores for completed matches

---

## 🚀 Next Steps

1. ✅ **Explore the admin interface** - Add your own teams/players
2. ✅ **Test the public pages** - See how fans will view data
3. ✅ **Customize branding** - Add your league name/logo/colors
4. ✅ **Add authentication** - Secure admin pages (Phase 3)
5. ✅ **Deploy to production** - Host on Azure/AWS/Heroku

---

## 📖 Additional Resources

- **API Documentation**: http://localhost:5207/swagger
- **Angular Docs**: https://angular.dev
- **.NET Docs**: https://learn.microsoft.com/dotnet/
- **Bootstrap 5**: https://getbootstrap.com/docs/5.3/
- **EF Core**: https://learn.microsoft.com/ef/core/

---

## 🎉 You're All Set!

Your iDiski League Management System is ready to use!

- **Admin Portal**: Manage teams, players, matches
- **Public Site**: Fans browse teams, fixtures, standings
- **API**: RESTful backend with Swagger docs
- **Database**: PostgreSQL with sample data

**Need help?** Check the troubleshooting section above or review the code comments.

---

**Built with ❤️ using .NET 9, Angular 21, PostgreSQL, and Bootstrap 5**
