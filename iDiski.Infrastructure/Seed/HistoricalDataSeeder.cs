using iDiski.Domain.Entities;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Seeds historical football data with proper UTC timestamps:
/// - PSL 2015/16 (South African Premier Soccer League)
/// - Premier League 2012/13 (English Premier League)
/// - WPL 2019/20 (Women's Premier League)
/// </summary>
public static class HistoricalDataSeeder
{
    public static async Task SeedHistoricalData(LeagueDbContext context)
    {
        Console.WriteLine("Starting historical data seeding...");

        // 1. PSL 2015/16 Season
        await SeedPSL2015_16(context);

        // 2. Premier League 2012/13 Season
        await SeedPremierLeague2012_13(context);

        // 3. WPL 2019/20 Season
        await SeedWPL2019_20(context);

        Console.WriteLine("Historical data seeding completed!");
    }

    #region PSL 2015/16

    private static async Task SeedPSL2015_16(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding PSL 2015/16 ===");

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
            Description = "South African Premier Soccer League 2015/16 season"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        // PSL Teams from 2015/16 season
        var teams = new List<Team>
        {
            new Team { Name = "Mamelodi Sundowns", ShortCode = "SUN", Founded = 1970, City = "Pretoria", HomeGround = "Loftus Versfeld", PrimaryColour = "#FFFF00", SecondaryColour = "#0000FF", DivisionId = division.Id },
            new Team { Name = "Kaizer Chiefs", ShortCode = "CHI", Founded = 1970, City = "Johannesburg", HomeGround = "FNB Stadium", PrimaryColour = "#FFD700", SecondaryColour = "#000000", DivisionId = division.Id },
            new Team { Name = "Orlando Pirates", ShortCode = "PIR", Founded = 1937, City = "Johannesburg", HomeGround = "Orlando Stadium", PrimaryColour = "#000000", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Bidvest Wits", ShortCode = "WIT", Founded = 1921, City = "Johannesburg", HomeGround = "Bidvest Stadium", PrimaryColour = "#0047AB", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "SuperSport United", ShortCode = "SSU", Founded = 1994, City = "Atteridgeville", HomeGround = "Lucas Moripe Stadium", PrimaryColour = "#0000FF", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Ajax Cape Town", ShortCode = "AJX", Founded = 1999, City = "Cape Town", HomeGround = "Cape Town Stadium", PrimaryColour = "#FF0000", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Free State Stars", ShortCode = "STA", Founded = 1977, City = "Bethlehem", HomeGround = "Goble Park", PrimaryColour = "#FFD700", SecondaryColour = "#000080", DivisionId = division.Id },
            new Team { Name = "Polokwane City", ShortCode = "POL", Founded = 2012, City = "Polokwane", HomeGround = "Peter Mokaba Stadium", PrimaryColour = "#0000FF", SecondaryColour = "#FFFF00", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} PSL teams");

        // Add sample matches
        await AddPSLMatches(context, teams, division);
    }

    private static async Task AddPSLMatches(LeagueDbContext context, List<Team> teams, Division division)
    {
        var matches = new List<MatchResult>();
        var random = new Random(2016);

        var sundowns = teams.First(t => t.ShortCode == "SUN");
        var chiefs = teams.First(t => t.ShortCode == "CHI");

        matches.Add(new MatchResult
        {
            HomeTeamId = sundowns.Id, AwayTeamId = chiefs.Id, HomeScore = 3, AwayScore = 1,
            MatchDate = DateTime.SpecifyKind(new DateTime(2015, 11, 7), DateTimeKind.Utc),
            MatchweekNumber = 10, Season = 2015,
            Status = MatchStatus.Completed, DivisionId = division.Id, Venue = sundowns.HomeGround
        });

        int matchweek = 1;
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < Math.Min(i + 3, teams.Count); j++)
            {
                matches.Add(new MatchResult
                {
                    HomeTeamId = teams[i].Id,
                    AwayTeamId = teams[j].Id,
                    HomeScore = random.Next(0, 4),
                    AwayScore = random.Next(0, 3),
                    MatchDate = DateTime.SpecifyKind(new DateTime(2015, 8, 8), DateTimeKind.Utc).AddDays(matchweek * 7),
                    MatchweekNumber = matchweek,
                    Season = 2015,
                    Status = MatchStatus.Completed,
                    DivisionId = division.Id,
                    Venue = teams[i].HomeGround
                });
                matchweek++;
            }
        }

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} PSL matches");
    }

    #endregion

    #region Premier League 2012/13

    private static async Task SeedPremierLeague2012_13(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding Premier League 2012/13 ===");

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
            Description = "English Premier League 2012/13 season"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "Manchester United", ShortCode = "MUN", Founded = 1878, City = "Manchester", HomeGround = "Old Trafford", PrimaryColour = "#DA291C", SecondaryColour = "#FBE122", DivisionId = division.Id },
            new Team { Name = "Manchester City", ShortCode = "MCI", Founded = 1880, City = "Manchester", HomeGround = "Etihad Stadium", PrimaryColour = "#6CABDD", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Chelsea", ShortCode = "CHE", Founded = 1905, City = "London", HomeGround = "Stamford Bridge", PrimaryColour = "#034694", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Arsenal", ShortCode = "ARS", Founded = 1886, City = "London", HomeGround = "Emirates Stadium", PrimaryColour = "#EF0107", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Tottenham Hotspur", ShortCode = "TOT", Founded = 1882, City = "London", HomeGround = "White Hart Lane", PrimaryColour = "#132257", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Everton", ShortCode = "EVE", Founded = 1878, City = "Liverpool", HomeGround = "Goodison Park", PrimaryColour = "#003399", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Liverpool", ShortCode = "LIV", Founded = 1892, City = "Liverpool", HomeGround = "Anfield", PrimaryColour = "#C8102E", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "West Brom", ShortCode = "WBA", Founded = 1878, City = "West Bromwich", HomeGround = "The Hawthorns", PrimaryColour = "#122F67", SecondaryColour = "#FFFFFF", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} Premier League teams");

        await AddPremierLeagueMatches(context, teams, division);
    }

    private static async Task AddPremierLeagueMatches(LeagueDbContext context, List<Team> teams, Division division)
    {
        var matches = new List<MatchResult>();
        var random = new Random(2013);

        int matchweek = 1;
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < Math.Min(i + 3, teams.Count); j++)
            {
                matches.Add(new MatchResult
                {
                    HomeTeamId = teams[i].Id,
                    AwayTeamId = teams[j].Id,
                    HomeScore = random.Next(0, 4),
                    AwayScore = random.Next(0, 3),
                    MatchDate = DateTime.SpecifyKind(new DateTime(2012, 8, 18), DateTimeKind.Utc).AddDays(matchweek * 7),
                    MatchweekNumber = matchweek,
                    Season = 2012,
                    Status = MatchStatus.Completed,
                    DivisionId = division.Id,
                    Venue = teams[i].HomeGround
                });
                matchweek++;
            }
        }

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} Premier League matches");
    }

    #endregion

    #region WPL 2019/20

    private static async Task SeedWPL2019_20(LeagueDbContext context)
    {
        Console.WriteLine("\n=== Seeding WPL 2019/20 ===");

        var division = new Division
        {
            Name = "Women's Premier League",
            ShortCode = "WPL",
            Season = 2019,
            AgeGroup = "Senior",
            Gender = Gender.Female,
            IsActive = false,
            StartDate = DateTime.SpecifyKind(new DateTime(2019, 9, 1), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(new DateTime(2020, 5, 31), DateTimeKind.Utc),
            Description = "Women's Premier League 2019/20 season"
        };

        context.Divisions.Add(division);
        await context.SaveChangesAsync();

        var teams = new List<Team>
        {
            new Team { Name = "JVW FC", ShortCode = "JVW", Founded = 2006, City = "Johannesburg", HomeGround = "Wits Stadium", PrimaryColour = "#0000FF", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Mamelodi Sundowns Ladies", ShortCode = "SUNL", Founded = 2009, City = "Pretoria", HomeGround = "Lucas Moripe Stadium", PrimaryColour = "#FFFF00", SecondaryColour = "#0000FF", DivisionId = division.Id },
            new Team { Name = "University of Western Cape", ShortCode = "UWC", Founded = 2010, City = "Cape Town", HomeGround = "UWC Stadium", PrimaryColour = "#FFD700", SecondaryColour = "#0000FF", DivisionId = division.Id },
            new Team { Name = "First Touch FC", ShortCode = "FT", Founded = 2008, City = "Port Elizabeth", HomeGround = "Gelvandale Stadium", PrimaryColour = "#FF0000", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Croesus Ladies", ShortCode = "CRO", Founded = 2015, City = "Rustenburg", HomeGround = "Olympia Park", PrimaryColour = "#800080", SecondaryColour = "#FFFFFF", DivisionId = division.Id },
            new Team { Name = "Ma-Indies Ladies", ShortCode = "MAI", Founded = 2012, City = "Durban", HomeGround = "King Zwelithini Stadium", PrimaryColour = "#008000", SecondaryColour = "#FFD700", DivisionId = division.Id }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} WPL teams");

        await AddWPLMatches(context, teams, division);
    }

    private static async Task AddWPLMatches(LeagueDbContext context, List<Team> teams, Division division)
    {
        var matches = new List<MatchResult>();
        var random = new Random(2020);

        int matchweek = 1;
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                matches.Add(new MatchResult
                {
                    HomeTeamId = teams[i].Id,
                    AwayTeamId = teams[j].Id,
                    HomeScore = random.Next(0, 4),
                    AwayScore = random.Next(0, 3),
                    MatchDate = DateTime.SpecifyKind(new DateTime(2019, 9, 1), DateTimeKind.Utc).AddDays(matchweek * 7),
                    MatchweekNumber = matchweek,
                    Season = 2019,
                    Status = MatchStatus.Completed,
                    DivisionId = division.Id,
                    Venue = teams[i].HomeGround
                });
                matchweek++;
            }
        }

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} WPL matches");
    }

    #endregion
}
