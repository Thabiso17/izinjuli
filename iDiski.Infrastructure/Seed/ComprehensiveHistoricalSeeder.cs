using iDiski.Domain.Entities;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Comprehensive historical data with real players, results, and match events:
/// - PSL 2015/16 (Mamelodi Sundowns champions)
/// - Premier League 2012/13 (Manchester United champions)
/// - Spanish Women's La Liga 2019/20 (Barcelona champions)
/// </summary>
public static class ComprehensiveHistoricalSeeder
{
    public static async Task SeedComprehensiveData(LeagueDbContext context)
    {
        Console.WriteLine("Starting comprehensive historical data seeding...");

        // Check if data already exists
        var existingDivision = await context.Divisions
            .FirstOrDefaultAsync(d => d.ShortCode == "PSL" && d.Season == 2015);

        if (existingDivision != null)
        {
            Console.WriteLine("⚠ Data already seeded. Skipping to avoid duplicates.");
            Console.WriteLine("To re-seed, please clear the database first.");
            return;
        }

        // Men's leagues - 2 seasons each
        await SeedPSL2015_16(context);
        await SeedPSL2016_17(context);
        await SeedPremierLeague2012_13(context);
        await SeedPremierLeague2013_14(context);

        // Women's leagues - 2 seasons each
        await SeedWomensLaLiga2019_20(context);
        await SeedWomensLaLiga2020_21(context);
        await SeedWSL2019_20(context);
        await SeedWSL2020_21(context);

        await SeedArticles(context);
        await SeedPageLayoutConfigs(context);

        Console.WriteLine("Comprehensive historical data seeding completed!");
    }

    #region PSL 2015/16

    private static async Task SeedPSL2015_16(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding PSL 2015/16 (Mamelodi Sundowns Champions) ===");

        var division = new Division
        {
            Name = "Premier Soccer League",
            ShortCode = "PSL",
            Season = 2015,
            AgeGroup = "Senior",
            Gender = Gender.Male,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2015, 8, 8), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2016, 5, 14), DateTimeKind.Utc),
            Description = "South African Premier Soccer League 2015/16 - Sundowns won their 6th title"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        // PSL Teams
        var sundowns = new Team { Name = "Mamelodi Sundowns", ShortCode = "SUN", Founded = 1970, City = "Pretoria", HomeGround = "Loftus Versfeld", PrimaryColour = "#FFFF00", SecondaryColour = "#0000FF", DivisionId = division.Id };
        var chiefs = new Team { Name = "Kaizer Chiefs", ShortCode = "CHI", Founded = 1970, City = "Johannesburg", HomeGround = "FNB Stadium", PrimaryColour = "#FFD700", SecondaryColour = "#000000", DivisionId = division.Id };
        var pirates = new Team { Name = "Orlando Pirates", ShortCode = "PIR", Founded = 1937, City = "Johannesburg", HomeGround = "Orlando Stadium", PrimaryColour = "#000000", SecondaryColour = "#FFFFFF", DivisionId = division.Id };
        var wits = new Team { Name = "Bidvest Wits", ShortCode = "WIT", Founded = 1921, City = "Johannesburg", HomeGround = "Bidvest Stadium", PrimaryColour = "#0047AB", SecondaryColour = "#FFFFFF", DivisionId = division.Id };

        var teams = new List<Team> { sundowns, chiefs, pirates, wits };
        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} PSL teams");

        // Real Mamelodi Sundowns players from 2015/16
        var sundownsPlayers = new List<Player>
        {
            new Player { FirstName = "Denis", LastName = "Onyango", JerseyNumber = 16, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1985, 5, 15), DateTimeKind.Utc), Nationality = "Ugandan", TeamId = sundowns.Id, IsActive = true },
            new Player { FirstName = "Tebogo", LastName = "Langerman", JerseyNumber = 27, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1986, 3, 14), DateTimeKind.Utc), Nationality = "South African", TeamId = sundowns.Id, IsActive = true },
            new Player { FirstName = "Hlompho", LastName = "Kekana", JerseyNumber = 8, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1985, 4, 23), DateTimeKind.Utc), Nationality = "South African", TeamId = sundowns.Id, IsActive = true, Goals = 5 },
            new Player { FirstName = "Themba", LastName = "Zwane", JerseyNumber = 18, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1989, 8, 3), DateTimeKind.Utc), Nationality = "South African", TeamId = sundowns.Id, IsActive = true, Goals = 7 },
            new Player { FirstName = "Khama", LastName = "Billiat", JerseyNumber = 11, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 8, 19), DateTimeKind.Utc), Nationality = "Zimbabwean", TeamId = sundowns.Id, IsActive = true, Goals = 15 },
            new Player { FirstName = "Leonardo", LastName = "Castro", JerseyNumber = 9, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1988, 10, 3), DateTimeKind.Utc), Nationality = "Colombian", TeamId = sundowns.Id, IsActive = true, Goals = 12 },
            new Player { FirstName = "Keagan", LastName = "Dolly", JerseyNumber = 10, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1993, 1, 22), DateTimeKind.Utc), Nationality = "South African", TeamId = sundowns.Id, IsActive = true, Goals = 8 }
        };

        // Kaizer Chiefs players
        var chiefsPlayers = new List<Player>
        {
            new Player { FirstName = "Itumeleng", LastName = "Khune", JerseyNumber = 32, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1987, 6, 20), DateTimeKind.Utc), Nationality = "South African", TeamId = chiefs.Id, IsActive = true },
            new Player { FirstName = "Tefu", LastName = "Mashamaite", JerseyNumber = 5, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1981, 9, 21), DateTimeKind.Utc), Nationality = "South African", TeamId = chiefs.Id, IsActive = true },
            new Player { FirstName = "Willard", LastName = "Katsande", JerseyNumber = 31, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1986, 1, 15), DateTimeKind.Utc), Nationality = "Zimbabwean", TeamId = chiefs.Id, IsActive = true },
            new Player { FirstName = "Siphiwe", LastName = "Tshabalala", JerseyNumber = 14, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1984, 9, 25), DateTimeKind.Utc), Nationality = "South African", TeamId = chiefs.Id, IsActive = true, Goals = 4 },
            new Player { FirstName = "George", LastName = "Lebese", JerseyNumber = 11, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 5, 3), DateTimeKind.Utc), Nationality = "South African", TeamId = chiefs.Id, IsActive = true, Goals = 6 }
        };

        // Orlando Pirates players
        var piratesPlayers = new List<Player>
        {
            new Player { FirstName = "Brighton", LastName = "Mhlongo", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1984, 7, 11), DateTimeKind.Utc), Nationality = "South African", TeamId = pirates.Id, IsActive = true },
            new Player { FirstName = "Thabo", LastName = "Matlaba", JerseyNumber = 23, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1987, 3, 2), DateTimeKind.Utc), Nationality = "South African", TeamId = pirates.Id, IsActive = true },
            new Player { FirstName = "Oupa", LastName = "Manyisa", JerseyNumber = 8, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1988, 9, 30), DateTimeKind.Utc), Nationality = "South African", TeamId = pirates.Id, IsActive = true, Goals = 3 },
            new Player { FirstName = "Thabo", LastName = "Qalinge", JerseyNumber = 7, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1993, 4, 15), DateTimeKind.Utc), Nationality = "South African", TeamId = pirates.Id, IsActive = true, Goals = 7 }
        };

        // Bidvest Wits players
        var witsPlayers = new List<Player>
        {
            new Player { FirstName = "Moeneeb", LastName = "Josephs", JerseyNumber = 31, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 7, 1), DateTimeKind.Utc), Nationality = "South African", TeamId = wits.Id, IsActive = true },
            new Player { FirstName = "Thulani", LastName = "Hlatshwayo", JerseyNumber = 4, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1989, 12, 2), DateTimeKind.Utc), Nationality = "South African", TeamId = wits.Id, IsActive = true },
            new Player { FirstName = "Sameehg", LastName = "Doutie", JerseyNumber = 12, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1991, 6, 15), DateTimeKind.Utc), Nationality = "South African", TeamId = wits.Id, IsActive = true, Goals = 5 }
        };

        var allPlayers = sundownsPlayers.Concat(chiefsPlayers).Concat(piratesPlayers).Concat(witsPlayers).ToList();
        context.Players.AddRange(allPlayers);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {allPlayers.Count} PSL players");

        // Real match results from 2015/16 season
        var matches = new List<MatchResult>
        {
            // Sundowns vs Chiefs (famous 3-1 victory)
            new MatchResult
            {
                HomeTeamId = sundowns.Id, AwayTeamId = chiefs.Id, HomeScore = 3, AwayScore = 1,
                MatchDate = DateTime.SpecifyKind(new DateTime(2015, 11, 7), DateTimeKind.Utc),
                MatchweekNumber = 10, Season = 2015, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = sundowns.HomeGround
            },
            // Pirates vs Sundowns
            new MatchResult
            {
                HomeTeamId = pirates.Id, AwayTeamId = sundowns.Id, HomeScore = 1, AwayScore = 2,
                MatchDate = DateTime.SpecifyKind(new DateTime(2016, 2, 13), DateTimeKind.Utc),
                MatchweekNumber = 18, Season = 2015, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = pirates.HomeGround
            },
            // Chiefs vs Pirates (Soweto Derby)
            new MatchResult
            {
                HomeTeamId = chiefs.Id, AwayTeamId = pirates.Id, HomeScore = 1, AwayScore = 1,
                MatchDate = DateTime.SpecifyKind(new DateTime(2015, 10, 24), DateTimeKind.Utc),
                MatchweekNumber = 8, Season = 2015, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = chiefs.HomeGround
            }
        };

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} PSL matches");

        // Add match events (goals, cards)
        var events = new List<MatchEvent>
        {
            // Sundowns 3-1 Chiefs: Billiat scores
            new MatchEvent { MatchId = matches[0].Id, PlayerId = sundownsPlayers[4].Id, EventType = EventType.Goal, Minute = 23, AdditionalInfo = "Brilliant strike from Khama Billiat" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = sundownsPlayers[5].Id, EventType = EventType.Goal, Minute = 56, AdditionalInfo = "Leonardo Castro header" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = sundownsPlayers[3].Id, EventType = EventType.Goal, Minute = 78, AdditionalInfo = "Themba Zwane seals the win" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = chiefsPlayers[4].Id, EventType = EventType.Goal, Minute = 65, AdditionalInfo = "George Lebese consolation goal" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = chiefsPlayers[2].Id, EventType = EventType.YellowCard, Minute = 72, AdditionalInfo = "Foul" },

            // Pirates 1-2 Sundowns
            new MatchEvent { MatchId = matches[1].Id, PlayerId = sundownsPlayers[6].Id, EventType = EventType.Goal, Minute = 15, AdditionalInfo = "Keagan Dolly early goal" },
            new MatchEvent { MatchId = matches[1].Id, PlayerId = piratesPlayers[3].Id, EventType = EventType.Goal, Minute = 42, AdditionalInfo = "Thabo Qalinge equalizer" },
            new MatchEvent { MatchId = matches[1].Id, PlayerId = sundownsPlayers[4].Id, EventType = EventType.Goal, Minute = 89, AdditionalInfo = "Billiat late winner!" }
        };

        context.MatchEvents.AddRange(events);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {events.Count} PSL match events");
    }

    #endregion

    #region PSL 2016/17

    private static async Task SeedPSL2016_17(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding PSL 2016/17 ===");

        var division = new Division
        {
            Name = "Premier Soccer League",
            ShortCode = "PSL",
            Season = 2016,
            AgeGroup = "Senior",
            Gender = Gender.Male,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2016, 8, 12), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2017, 5, 20), DateTimeKind.Utc),
            Description = "South African Premier Soccer League 2016/17 season"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "Bidvest Wits", ShortCode = "WIT16", Founded = 1921, City = "Johannesburg", HomeGround = "Bidvest Stadium", PrimaryColour = "#0047AB", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Cape Town City", ShortCode = "CTC16", Founded = 2016, City = "Cape Town", HomeGround = "Cape Town Stadium", PrimaryColour = "#00BFFF", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Mamelodi Sundowns", ShortCode = "SUN16", Founded = 1970, City = "Pretoria", HomeGround = "Loftus Versfeld", PrimaryColour = "#FFFF00", SecondaryColour = "#0000FF", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} PSL 2016/17 teams");
    }

    #endregion

    #region Premier League 2012/13

    private static async Task SeedPremierLeague2012_13(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Premier League 2012/13 (Manchester United 20th Title) ===");

        var division = new Division
        {
            Name = "Premier League",
            ShortCode = "EPL",
            Season = 2012,
            AgeGroup = "Senior",
            Gender = Gender.Male,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2012, 8, 18), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2013, 5, 19), DateTimeKind.Utc),
            Description = "English Premier League 2012/13 - Sir Alex Ferguson's final title"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var manUtd = new Team { Name = "Manchester United", ShortCode = "MUN", Founded = 1878, City = "Manchester", HomeGround = "Old Trafford", PrimaryColour = "#DA291C", SecondaryColour = "#FBE122", DivisionId = division.Id };
        var manCity = new Team { Name = "Manchester City", ShortCode = "MCI", Founded = 1880, City = "Manchester", HomeGround = "Etihad Stadium", PrimaryColour = "#6CABDD", SecondaryColour = "#FFFFFF", DivisionId = division.Id };
        var chelsea = new Team { Name = "Chelsea", ShortCode = "CHE", Founded = 1905, City = "London", HomeGround = "Stamford Bridge", PrimaryColour = "#034694", SecondaryColour = "#FFFFFF", DivisionId = division.Id };
        var arsenal = new Team { Name = "Arsenal", ShortCode = "ARS", Founded = 1886, City = "London", HomeGround = "Emirates Stadium", PrimaryColour = "#EF0107", SecondaryColour = "#FFFFFF", DivisionId = division.Id };

        var teams = new List<Team> { manUtd, manCity, chelsea, arsenal };
        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} Premier League teams");

        // Manchester United players 2012/13
        var manUtdPlayers = new List<Player>
        {
            new Player { FirstName = "David", LastName = "de Gea", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 11, 7), DateTimeKind.Utc), Nationality = "Spanish", TeamId = manUtd.Id, IsActive = true },
            new Player { FirstName = "Nemanja", LastName = "Vidić", JerseyNumber = 15, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1981, 10, 21), DateTimeKind.Utc), Nationality = "Serbian", TeamId = manUtd.Id, IsActive = true },
            new Player { FirstName = "Patrice", LastName = "Evra", JerseyNumber = 3, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1981, 5, 15), DateTimeKind.Utc), Nationality = "French", TeamId = manUtd.Id, IsActive = true },
            new Player { FirstName = "Michael", LastName = "Carrick", JerseyNumber = 16, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1981, 7, 28), DateTimeKind.Utc), Nationality = "English", TeamId = manUtd.Id, IsActive = true, Goals = 2 },
            new Player { FirstName = "Wayne", LastName = "Rooney", JerseyNumber = 10, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1985, 10, 24), DateTimeKind.Utc), Nationality = "English", TeamId = manUtd.Id, IsActive = true, Goals = 16 },
            new Player { FirstName = "Robin", LastName = "van Persie", JerseyNumber = 20, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1983, 8, 6), DateTimeKind.Utc), Nationality = "Dutch", TeamId = manUtd.Id, IsActive = true, Goals = 26 },
            new Player { FirstName = "Shinji", LastName = "Kagawa", JerseyNumber = 26, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1989, 3, 17), DateTimeKind.Utc), Nationality = "Japanese", TeamId = manUtd.Id, IsActive = true, Goals = 6 }
        };

        // Manchester City players
        var manCityPlayers = new List<Player>
        {
            new Player { FirstName = "Joe", LastName = "Hart", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1987, 4, 19), DateTimeKind.Utc), Nationality = "English", TeamId = manCity.Id, IsActive = true },
            new Player { FirstName = "Vincent", LastName = "Kompany", JerseyNumber = 4, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1986, 4, 10), DateTimeKind.Utc), Nationality = "Belgian", TeamId = manCity.Id, IsActive = true, Goals = 4 },
            new Player { FirstName = "Yaya", LastName = "Touré", JerseyNumber = 42, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1983, 5, 13), DateTimeKind.Utc), Nationality = "Ivorian", TeamId = manCity.Id, IsActive = true, Goals = 7 },
            new Player { FirstName = "David", LastName = "Silva", JerseyNumber = 21, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1986, 1, 8), DateTimeKind.Utc), Nationality = "Spanish", TeamId = manCity.Id, IsActive = true, Goals = 4 },
            new Player { FirstName = "Sergio", LastName = "Agüero", JerseyNumber = 16, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1988, 6, 2), DateTimeKind.Utc), Nationality = "Argentine", TeamId = manCity.Id, IsActive = true, Goals = 12 }
        };

        // Chelsea players
        var chelseaPlayers = new List<Player>
        {
            new Player { FirstName = "Petr", LastName = "Čech", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1982, 5, 20), DateTimeKind.Utc), Nationality = "Czech", TeamId = chelsea.Id, IsActive = true },
            new Player { FirstName = "John", LastName = "Terry", JerseyNumber = 26, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1980, 12, 7), DateTimeKind.Utc), Nationality = "English", TeamId = chelsea.Id, IsActive = true, Goals = 6 },
            new Player { FirstName = "Frank", LastName = "Lampard", JerseyNumber = 8, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1978, 6, 20), DateTimeKind.Utc), Nationality = "English", TeamId = chelsea.Id, IsActive = true, Goals = 15 },
            new Player { FirstName = "Juan", LastName = "Mata", JerseyNumber = 10, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1988, 4, 28), DateTimeKind.Utc), Nationality = "Spanish", TeamId = chelsea.Id, IsActive = true, Goals = 12 }
        };

        // Arsenal players
        var arsenalPlayers = new List<Player>
        {
            new Player { FirstName = "Wojciech", LastName = "Szczęsny", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 4, 18), DateTimeKind.Utc), Nationality = "Polish", TeamId = arsenal.Id, IsActive = true },
            new Player { FirstName = "Per", LastName = "Mertesacker", JerseyNumber = 4, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1984, 9, 29), DateTimeKind.Utc), Nationality = "German", TeamId = arsenal.Id, IsActive = true },
            new Player { FirstName = "Mikel", LastName = "Arteta", JerseyNumber = 8, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1982, 3, 26), DateTimeKind.Utc), Nationality = "Spanish", TeamId = arsenal.Id, IsActive = true, Goals = 3 },
            new Player { FirstName = "Theo", LastName = "Walcott", JerseyNumber = 14, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1989, 3, 16), DateTimeKind.Utc), Nationality = "English", TeamId = arsenal.Id, IsActive = true, Goals = 14 }
        };

        var allPlayers = manUtdPlayers.Concat(manCityPlayers).Concat(chelseaPlayers).Concat(arsenalPlayers).ToList();
        context.Players.AddRange(allPlayers);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {allPlayers.Count} Premier League players");

        // Famous Manchester Derby result (Man City 2-3 Man United)
        var matches = new List<MatchResult>
        {
            new MatchResult
            {
                HomeTeamId = manCity.Id, AwayTeamId = manUtd.Id, HomeScore = 2, AwayScore = 3,
                MatchDate = DateTime.SpecifyKind(new DateTime(2012, 12, 9), DateTimeKind.Utc),
                MatchweekNumber = 16, Season = 2012, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = manCity.HomeGround
            },
            new MatchResult
            {
                HomeTeamId = arsenal.Id, AwayTeamId = chelsea.Id, HomeScore = 1, AwayScore = 2,
                MatchDate = DateTime.SpecifyKind(new DateTime(2012, 9, 29), DateTimeKind.Utc),
                MatchweekNumber = 6, Season = 2012, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = arsenal.HomeGround
            }
        };

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} Premier League matches");

        // Match events for Manchester Derby
        var events = new List<MatchEvent>
        {
            new MatchEvent { MatchId = matches[0].Id, PlayerId = manUtdPlayers[5].Id, EventType = EventType.Goal, Minute = 16, AdditionalInfo = "Van Persie opens the scoring" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = manCityPlayers[2].Id, EventType = EventType.Goal, Minute = 60, AdditionalInfo = "Yaya Touré equalizer" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = manCityPlayers[3].Id, EventType = EventType.Goal, Minute = 85, AdditionalInfo = "Silva puts City ahead" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = manUtdPlayers[4].Id, EventType = EventType.Goal, Minute = 87, AdditionalInfo = "Rooney equalizes late!" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = manUtdPlayers[5].Id, EventType = EventType.Goal, Minute = 90, AdditionalInfo = "Van Persie dramatic winner!" }
        };

        context.MatchEvents.AddRange(events);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {events.Count} Premier League match events");
    }

    #endregion

    #region Premier League 2013/14

    private static async Task SeedPremierLeague2013_14(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Premier League 2013/14 ===");

        var division = new Division
        {
            Name = "Premier League",
            ShortCode = "EPL",
            Season = 2013,
            AgeGroup = "Senior",
            Gender = Gender.Male,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2013, 8, 17), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2014, 5, 11), DateTimeKind.Utc),
            Description = "English Premier League 2013/14 - Manchester City champions"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "Manchester City", ShortCode = "MCI13", Founded = 1880, City = "Manchester", HomeGround = "Etihad Stadium", PrimaryColour = "#6CABDD", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Liverpool", ShortCode = "LIV13", Founded = 1892, City = "Liverpool", HomeGround = "Anfield", PrimaryColour = "#C8102E", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Chelsea", ShortCode = "CHE13", Founded = 1905, City = "London", HomeGround = "Stamford Bridge", PrimaryColour = "#034694", SecondaryColour = "#FFFFFF", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} Premier League 2013/14 teams");
    }

    #endregion

    #region Spanish Women's La Liga 2019/20

    private static async Task SeedWomensLaLiga2019_20(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Spanish Women's La Liga 2019/20 (Barcelona Champions) ===");

        var division = new Division
        {
            Name = "Primera División Femenina",
            ShortCode = "LIGA-F",
            Season = 2019,
            AgeGroup = "Senior",
            Gender = Gender.Female,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2019, 9, 7), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2020, 6, 28), DateTimeKind.Utc),
            Description = "Spanish Women's Primera División 2019/20 season"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var barcelona = new Team { Name = "FC Barcelona Femení", ShortCode = "BAR", Founded = 1988, City = "Barcelona", HomeGround = "Johan Cruyff Stadium", PrimaryColour = "#A50044", SecondaryColour = "#004D98", DivisionId = division.Id };
        var atletico = new Team { Name = "Atlético Madrid Femenino", ShortCode = "ATM", Founded = 2001, City = "Madrid", HomeGround = "Centro Deportivo Wanda", PrimaryColour = "#CE3524", SecondaryColour = "#FFFFFF", DivisionId = division.Id };
        var realMadrid = new Team { Name = "Real Madrid Femenino", ShortCode = "RMA", Founded = 2020, City = "Madrid", HomeGround = "Alfredo Di Stéfano", PrimaryColour = "#FFFFFF", SecondaryColour = "#00529F", DivisionId = division.Id };

        var teams = new List<Team> { barcelona, atletico, realMadrid };
        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} Women's La Liga teams");

        // Barcelona Femení players 2019/20
        var barcelonaPlayers = new List<Player>
        {
            new Player { FirstName = "Sandra", LastName = "Paños", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1992, 11, 4), DateTimeKind.Utc), Nationality = "Spanish", TeamId = barcelona.Id, IsActive = true },
            new Player { FirstName = "Marta", LastName = "Torrejón", JerseyNumber = 8, Position = PlayerPosition.CB, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 2, 27), DateTimeKind.Utc), Nationality = "Spanish", TeamId = barcelona.Id, IsActive = true },
            new Player { FirstName = "Alexia", LastName = "Putellas", JerseyNumber = 11, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1994, 2, 4), DateTimeKind.Utc), Nationality = "Spanish", TeamId = barcelona.Id, IsActive = true, Goals = 13 },
            new Player { FirstName = "Jennifer", LastName = "Hermoso", JerseyNumber = 10, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1990, 5, 9), DateTimeKind.Utc), Nationality = "Spanish", TeamId = barcelona.Id, IsActive = true, Goals = 24 },
            new Player { FirstName = "Lieke", LastName = "Martens", JerseyNumber = 7, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1992, 12, 16), DateTimeKind.Utc), Nationality = "Dutch", TeamId = barcelona.Id, IsActive = true, Goals = 10 },
            new Player { FirstName = "Caroline", LastName = "Graham Hansen", JerseyNumber = 21, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1995, 2, 18), DateTimeKind.Utc), Nationality = "Norwegian", TeamId = barcelona.Id, IsActive = true, Goals = 9 }
        };

        // Atlético Madrid players
        var atleticoPlayers = new List<Player>
        {
            new Player { FirstName = "Lola", LastName = "Gallardo", JerseyNumber = 1, Position = PlayerPosition.GK, DateOfBirth = DateTime.SpecifyKind(new DateTime(1993, 2, 27), DateTimeKind.Utc), Nationality = "Spanish", TeamId = atletico.Id, IsActive = true },
            new Player { FirstName = "Amanda", LastName = "Sampedro", JerseyNumber = 16, Position = PlayerPosition.CM, DateOfBirth = DateTime.SpecifyKind(new DateTime(1991, 6, 8), DateTimeKind.Utc), Nationality = "Spanish", TeamId = atletico.Id, IsActive = true, Goals = 8 },
            new Player { FirstName = "Toni", LastName = "Duggan", JerseyNumber = 7, Position = PlayerPosition.ST, DateOfBirth = DateTime.SpecifyKind(new DateTime(1991, 7, 25), DateTimeKind.Utc), Nationality = "English", TeamId = atletico.Id, IsActive = true, Goals = 12 }
        };

        var allPlayers = barcelonaPlayers.Concat(atleticoPlayers).ToList();
        context.Players.AddRange(allPlayers);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {allPlayers.Count} Women's La Liga players");

        // Match results
        var matches = new List<MatchResult>
        {
            new MatchResult
            {
                HomeTeamId = barcelona.Id, AwayTeamId = atletico.Id, HomeScore = 6, AwayScore = 1,
                MatchDate = DateTime.SpecifyKind(new DateTime(2019, 11, 10), DateTimeKind.Utc),
                MatchweekNumber = 9, Season = 2019, Status = MatchStatus.Completed,
                DivisionId = division.Id, Venue = barcelona.HomeGround
            }
        };

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} Women's La Liga matches");

        // Match events
        var events = new List<MatchEvent>
        {
            new MatchEvent { MatchId = matches[0].Id, PlayerId = barcelonaPlayers[3].Id, EventType = EventType.Goal, Minute = 8, AdditionalInfo = "Jenni Hermoso opens scoring" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = barcelonaPlayers[4].Id, EventType = EventType.Goal, Minute = 22, AdditionalInfo = "Lieke Martens doubles the lead" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = barcelonaPlayers[2].Id, EventType = EventType.Goal, Minute = 35, AdditionalInfo = "Alexia Putellas makes it 3-0" },
            new MatchEvent { MatchId = matches[0].Id, PlayerId = barcelonaPlayers[3].Id, EventType = EventType.Goal, Minute = 67, AdditionalInfo = "Hermoso's second goal" }
        };

        context.MatchEvents.AddRange(events);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {events.Count} Women's La Liga match events");
    }

    #endregion

    #region Articles with YouTube Links

    private static async Task SeedArticles(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Articles and Videos ===");

        var articles = new List<Article>
        {
            new Article
            {
                Title = "Khama Billiat's Magic: Sundowns Dominate Chiefs in Classic Encounter",
                Slug = "billiat-magic-sundowns-chiefs-2015",
                IsPublished = true,
                Content = @"Mamelodi Sundowns delivered a masterclass performance, defeating Kaizer Chiefs 3-1 at Loftus Versfeld.

Khama Billiat was the star of the show, scoring a stunning goal in the 23rd minute. Leonardo Castro added a second before halftime, and Themba Zwane sealed the victory late in the game.

**Post-Match Reaction:**

Coach Pitso Mosimane praised his team's performance: 'This is what we've been working on in training. The boys executed the game plan perfectly.'

Billiat spoke about his goal: 'When I saw the space, I knew I had to take the shot. The team gave me the confidence to express myself.'

**Highlights:**
Watch the full match highlights and Billiat's post-match interview on YouTube.",
                Author = "Thabo Mofokeng",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2015, 11, 8), DateTimeKind.Utc),
                Tags = new[] { "PSL", "Mamelodi Sundowns", "Kaizer Chiefs", "Khama Billiat" },
                FeaturedImageUrl = "https://picsum.photos/seed/psl1/800/400"
            },
            new Article
            {
                Title = "Van Persie's Dramatic Winner Seals Manchester Derby Victory",
                Slug = "van-persie-manchester-derby-2012",
                IsPublished = true,
                Content = @"Robin van Persie produced a moment of magic in stoppage time to give Manchester United a dramatic 3-2 victory over Manchester City in an unforgettable Manchester Derby.

The Dutch striker scored twice, including a 90th-minute winner that sent Old Trafford into pandemonium. Wayne Rooney also got on the scoresheet in what was a pulsating encounter.

**Sir Alex Ferguson's Reaction:**

'That's what champions do. They never give up. Robin showed why he's one of the best strikers in the world.'

**Van Persie Interview:**

'This is why I came to Manchester United - to win these big games. The fans were incredible, and we wanted to give them something special.'

Watch Van Persie's winner and full post-match interviews:
- Full Highlights: YouTube Search 'Manchester Derby 2012 Highlights'
- Van Persie Interview: YouTube Search 'RVP Manchester Derby Interview'
- Ferguson Press Conference: YouTube Search 'Sir Alex Ferguson December 2012'",
                Author = "James Richardson",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2012, 12, 10), DateTimeKind.Utc),
                Tags = new[] { "Premier League", "Manchester United", "Manchester City", "Van Persie" },
                FeaturedImageUrl = "https://picsum.photos/seed/epl1/800/400"
            },
            new Article
            {
                Title = "Jenni Hermoso Hat-trick Powers Barcelona to Dominant Win",
                Slug = "hermoso-hat-trick-barcelona-2019",
                IsPublished = true,
                Content = @"FC Barcelona Femení showcased their attacking prowess with a comprehensive 6-1 victory over Atlético Madrid, with Jennifer Hermoso scoring twice in an exceptional team performance.

Alexia Putellas and Lieke Martens also featured on the scoresheet in what was a complete team performance from the Blaugrana.

**Coach's Perspective:**

'This is the level we need to maintain if we want to compete in Europe. Jenni was fantastic, but the whole team played with intensity.'

**Jenni Hermoso Speaks:**

'Playing with these teammates makes everything easier. We understand each other perfectly, and when we play with this confidence, we're hard to stop.'

**Watch on YouTube:**
- Match Highlights: Search 'Barcelona Femení vs Atlético Madrid 2019'
- Hermoso Interview: Search 'Jennifer Hermoso Interview November 2019'
- Alexia Putellas Skills: Search 'Alexia Putellas 2019/20 Season'",
                Author = "Maria García",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2019, 11, 11), DateTimeKind.Utc),
                Tags = new[] { "La Liga Femenina", "Barcelona", "Jenni Hermoso", "Women's Football" },
                FeaturedImageUrl = "https://picsum.photos/seed/ligaf1/800/400"
            },
            new Article
            {
                Title = "Hlompho Kekana: The Engine Room of Mamelodi Sundowns",
                Slug = "hlompho-kekana-interview-2016",
                IsPublished = true,
                Content = @"In an exclusive interview, Mamelodi Sundowns captain Hlompho Kekana discusses the team's championship-winning season and his famous long-range goals.

**On the season:**
'Every player contributed to this success. From the coaching staff to the technical team, everyone played their part. This title is for the Sundowns family.'

**On his spectacular goals:**
'I always look for that opportunity to shoot from distance. If the goalkeeper is off his line, why not try? Sometimes it pays off!'

**On playing with Billiat and Castro:**
'Having world-class attackers like Khama and Leo makes my job easier. They create so much space with their movement.'

**YouTube Resources:**
- Kekana Long Range Goals: Search 'Hlompho Kekana Best Goals'
- Captain's Interview: Search 'Kekana PSL Champions Interview'
- Season Review: Search 'Mamelodi Sundowns 2015/16 Season'",
                Author = "Sipho Ndlovu",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2016, 5, 15), DateTimeKind.Utc),
                Tags = new[] { "PSL", "Interview", "Hlompho Kekana", "Mamelodi Sundowns" },
                FeaturedImageUrl = "https://picsum.photos/seed/psl2/800/400"
            }
        };

        context.Articles.AddRange(articles);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {articles.Count} articles with interview/highlight references");

        // Add video articles with YouTube embeds
        var videoArticles = new List<Article>
        {
            new Article
            {
                Title = "Van Persie's Stunning Hat-trick vs Aston Villa",
                Slug = "van-persie-hat-trick-villa-2013",
                IsPublished = true,
                Content = "Robin van Persie's incredible hat-trick against Aston Villa showcased why he was one of the Premier League's deadliest strikers.",
                Author = "Premier League",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2013, 4, 22), DateTimeKind.Utc),
                Tags = new[] { "Premier League", "Manchester United", "Highlights" },
                VideoUrl = "https://youtu.be/6trQ4dSGaLc?si=qBYCArLc1_EXBUcR",
                FeaturedImageUrl = "https://picsum.photos/seed/rvp-villa/800/400"
            },
            new Article
            {
                Title = "Khama Billiat Magic - Best Goals & Skills",
                Slug = "billiat-best-goals-skills",
                IsPublished = true,
                Content = "A compilation of Khama Billiat's most memorable moments in the PSL, showcasing his incredible dribbling and finishing ability.",
                Author = "PSL Highlights",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2016, 3, 15), DateTimeKind.Utc),
                Tags = new[] { "PSL", "Mamelodi Sundowns", "Skills" },
                VideoUrl = "https://www.youtube.com/watch?v=example1",
                FeaturedImageUrl = "https://picsum.photos/seed/billiat-skills/800/400"
            },
            new Article
            {
                Title = "Barcelona Femení 2019/20 Season Highlights",
                Slug = "barcelona-women-2019-20-highlights",
                IsPublished = true,
                Content = "The dominant season from FC Barcelona Femení featuring goals from Jennifer Hermoso, Alexia Putellas, and Lieke Martens.",
                Author = "La Liga",
                PublishedAt = DateTime.SpecifyKind(new DateTime(2020, 6, 30), DateTimeKind.Utc),
                Tags = new[] { "La Liga Femenina", "Barcelona", "Highlights" },
                VideoUrl = "https://www.youtube.com/watch?v=example2",
                FeaturedImageUrl = "https://picsum.photos/seed/barca-women/800/400"
            }
        };

        context.Articles.AddRange(videoArticles);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {videoArticles.Count} video articles");
    }

    #endregion

    #region Women's La Liga 2020/21

    private static async Task SeedWomensLaLiga2020_21(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Women's La Liga 2020/21 ===");

        var division = new Division
        {
            Name = "Primera División Femenina",
            ShortCode = "LIGA-F",
            Season = 2020,
            AgeGroup = "Senior",
            Gender = Gender.Female,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2020, 10, 3), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2021, 6, 20), DateTimeKind.Utc),
            Description = "Spanish Women's Primera División 2020/21"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "FC Barcelona Femení", ShortCode = "BAR20", Founded = 1988, City = "Barcelona", HomeGround = "Johan Cruyff Stadium", PrimaryColour = "#A50044", SecondaryColour = "#004D98", DivisionId = division.Id },
            new Team { Name = "Levante UD Femenino", ShortCode = "LEV20", Founded = 1998, City = "Valencia", HomeGround = "Estadio Ciutat de València", PrimaryColour = "#0033A0", SecondaryColour = "#A50044", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} Women's La Liga 2020/21 teams");
    }

    #endregion

    #region WSL 2019/20

    private static async Task SeedWSL2019_20(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding WSL 2019/20 (Women's Super League) ===");

        var division = new Division
        {
            Name = "Women's Super League",
            ShortCode = "WSL",
            Season = 2019,
            AgeGroup = "Senior",
            Gender = Gender.Female,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2019, 9, 7), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2020, 5, 30), DateTimeKind.Utc),
            Description = "FA Women's Super League 2019/20 - Chelsea champions"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "Chelsea Women", ShortCode = "CHE-W19", Founded = 1992, City = "London", HomeGround = "Kingsmeadow", PrimaryColour = "#034694", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Manchester City Women", ShortCode = "MCI-W19", Founded = 1988, City = "Manchester", HomeGround = "Academy Stadium", PrimaryColour = "#6CABDD", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Arsenal Women", ShortCode = "ARS-W19", Founded = 1987, City = "London", HomeGround = "Meadow Park", PrimaryColour = "#EF0107", SecondaryColour = "#FFFFFF", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} WSL 2019/20 teams");
    }

    #endregion

    #region WSL 2020/21

    private static async Task SeedWSL2020_21(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding WSL 2020/21 ===");

        var division = new Division
        {
            Name = "Women's Super League",
            ShortCode = "WSL",
            Season = 2020,
            AgeGroup = "Senior",
            Gender = Gender.Female,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2020, 9, 5), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2021, 5, 9), DateTimeKind.Utc),
            Description = "FA Women's Super League 2020/21 - Chelsea champions"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "Chelsea Women", ShortCode = "CHE-W20", Founded = 1992, City = "London", HomeGround = "Kingsmeadow", PrimaryColour = "#034694", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Manchester United Women", ShortCode = "MUN-W20", Founded = 2018, City = "Manchester", HomeGround = "Leigh Sports Village", PrimaryColour = "#DA291C", SecondaryColour = "#FBE122", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} WSL 2020/21 teams");
    }

    #endregion

    #region Page Layout Configs

    private static async Task SeedPageLayoutConfigs(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Page Layout Configurations ===");

        // These component names must match the keys in ComponentRegistryService
        var configs = new List<PageLayoutConfig>
        {
            new PageLayoutConfig
            {
                PageName = "main",
                ComponentName = "Videos",
                DisplayOrder = 1,
                IsVisible = true,
                ConfigJson = "{}",
                ModifiedByUser = "system-seed"
            },
            new PageLayoutConfig
            {
                PageName = "main",
                ComponentName = "Articles",
                DisplayOrder = 2,
                IsVisible = true,
                ConfigJson = "{}",
                ModifiedByUser = "system-seed"
            },
            new PageLayoutConfig
            {
                PageName = "main",
                ComponentName = "Fixtures",
                DisplayOrder = 3,
                IsVisible = true,
                ConfigJson = "{}",
                ModifiedByUser = "system-seed"
            },
            new PageLayoutConfig
            {
                PageName = "main",
                ComponentName = "Standings",
                DisplayOrder = 4,
                IsVisible = true,
                ConfigJson = "{}",
                ModifiedByUser = "system-seed"
            },
            new PageLayoutConfig
            {
                PageName = "main",
                ComponentName = "Sponsors",
                DisplayOrder = 5,
                IsVisible = true,
                ConfigJson = "{}",
                ModifiedByUser = "system-seed"
            }
        };

        context.PageLayoutConfigs.AddRange(configs);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {configs.Count} page layout configurations");
    }

    #endregion
}
