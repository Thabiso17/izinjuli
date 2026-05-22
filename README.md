# iZinjuli ⚽

**iZinjuli** (formerly iDiski) is a comprehensive football league management system built with .NET 9 and Angular 21. Manage teams, players, fixtures, standings, and more with a modern, responsive interface.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21.1.3-DD0031)](https://angular.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## 🌟 Features

### ⚡ Core Functionality
- **Team Management**: CRUD operations, logo uploads, division associations
- **Player Management**: 18 detailed positions, preferred foot, profile photos, jersey numbers
- **Match Management**: Fixture scheduling, score recording, match events
- **League Standings**: Auto-calculated with points, GD, GF, and form tracking
- **Divisions**: Season management with gender and age group support
- **Articles & News**: Blog-style content management with tags
- **Sponsors**: Multi-tier sponsorship with logo uploads and placements

### 🚀 Advanced Features
- **🔄 Player Transfers**: Move players between teams with jersey validation
- **⚙️ Fixture Generator**: Auto-generate round-robin fixtures (single or home-and-away)
- **📊 Standings Calculator**: Real-time league table with form tracking
- **📸 Image Upload System**: Local file storage for logos and photos
- **🔐 Admin Panel**: Complete management interface with responsive design

---

## 🏗️ Architecture

### Backend (.NET 9)
```
iDiski/
├── iDiski.Domain/          # Entities, Domain Services
├── iDiski.Application/     # CQRS Commands/Queries, DTOs
├── iDiski.Infrastructure/  # EF Core, Persistence, Migrations
└── iDiski.Api/            # Controllers, API endpoints
```

**Tech Stack:**
- **Clean Architecture** (Domain → Application → Infrastructure → API)
- **CQRS** pattern with MediatR
- **FluentValidation** for request validation
- **Entity Framework Core** with PostgreSQL
- **OpenAPI/Swagger** documentation

### Frontend (Angular 21)
```
iDiski-Client/
├── src/app/core/          # Models, Services
├── src/app/features/      # Feature modules (public + admin)
├── src/app/layouts/       # Header, Footer
└── src/environments/      # Environment configs
```

**Tech Stack:**
- **Angular 21.1.3** with standalone components
- **TypeScript** & **RxJS**
- **Bootstrap 5** for responsive UI
- **Angular Signals** for reactivity

---

## 🚀 Quick Start

### Prerequisites
- **.NET 9 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+**: [Download](https://nodejs.org/)
- **PostgreSQL 16+**: [Download](https://www.postgresql.org/download/)
- **Git**: [Download](https://git-scm.com/)

### 1️⃣ Clone the Repository
```bash
git clone https://github.com/Thabiso17/izinjuli.git
cd izinjuli
```

### 2️⃣ Setup Backend

1. **Update connection string** in `iDiski.Api/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Port=5432;Database=izinjuli_db;Username=postgres;Password=YOUR_PASSWORD"
     }
   }
   ```

2. **Apply migrations**:
   ```bash
   cd iDiski.Api
   dotnet ef database update --project ../iDiski.Infrastructure
   ```

3. **Run the API**:
   ```bash
   dotnet run
   ```
   API will be available at: `https://localhost:5001`

4. **Seed sample data** (optional):
   ```bash
   # Visit: https://localhost:5001/api/seed
   # Or: https://localhost:5001/api/seed/comprehensive
   ```

### 3️⃣ Setup Frontend

1. **Install dependencies**:
   ```bash
   cd iDiski-Client
   npm install
   ```

2. **Run dev server**:
   ```bash
   ng serve
   ```
   App will be available at: `http://localhost:4200`

---

## 📖 Documentation

- **[CLAUDE.md](CLAUDE.md)**: Development guide and architecture
- **[GETTING-STARTED.md](GETTING-STARTED.md)**: Detailed setup instructions
- **API Documentation**: Visit `/openapi/v1.json` when API is running

---

## 🎯 Key Features in Detail

### Player Management
- **18 Positions**: GK, CB, SW, RB, LB, RWB, LWB, CDM, CM, CAM, RM, LM, ST, CF, RW, LW
- **Preferred Foot**: Left, Right, Both
- **Division Filtering**: Filter players by division and team
- **Transfer System**: Move players between teams with validation

### Fixture Generator
- **Round-Robin Algorithm**: Smart team rotation
- **Single or Home-and-Away**: Configurable format
- **Automatic Scheduling**: Set start date and matchweek intervals
- **Odd Teams Support**: Handles BYE rounds automatically

### Image Upload
- **File Storage**: Local file system (`wwwroot/uploads/`)
- **Validation**: 5MB max, image types only
- **Organized Folders**: Separate folders for teams, players, sponsors
- **URL Fallback**: Still supports direct image URLs

---

## 🛠️ Development Commands

### Backend
```bash
# Build solution
dotnet build iDiski.sln

# Run API
dotnet run --project iDiski.Api

# Run tests
dotnet test

# Create migration
dotnet ef migrations add MigrationName --project iDiski.Infrastructure --startup-project iDiski.Api

# Update database
dotnet ef database update --project iDiski.Infrastructure --startup-project iDiski.Api
```

### Frontend
```bash
# Start dev server
ng serve

# Build for production
ng build

# Run tests
ng test

# Generate component
ng generate component component-name
```

---

## 📦 Database Seeding

The system includes comprehensive seed data:

1. **Basic Seed** (`/api/seed`):
   - Current season data
   - Sample teams, players, divisions

2. **Historical Seed** (`/api/seed/historical`):
   - PSL 2015/16 season
   - Premier League 2012/13 season
   - WPL 2019/20 season

3. **Comprehensive Seed** (`/api/seed/comprehensive`):
   - Real player names and stats
   - Complete match results
   - Match events (goals, cards, substitutions)

---

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with ❤️ using .NET 9 and Angular 21
- Clean Architecture pattern inspired by Jason Taylor
- Round-robin algorithm based on the circle method
- Co-developed with Claude Sonnet 4.5

---

## 📧 Contact

**Project Link**: [https://github.com/Thabiso17/izinjuli](https://github.com/Thabiso17/izinjuli)

---

## 🗺️ Roadmap

- [ ] User authentication and authorization
- [ ] Real-time match updates with SignalR
- [ ] Mobile app (React Native / Flutter)
- [ ] Advanced statistics and analytics
- [ ] Multi-league support
- [ ] Payment integration for registrations
- [ ] Social media integration
- [ ] PWA support for offline access

---

**Made with ⚡ by Thabiso**
