using iDiski.Domain.Entities;
using iDiski.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace iDiski.Infrastructure.Seed;

/// <summary>
/// Seeds the database with sample data for testing and demonstration.
/// Run this once to populate teams, players, divisions, and sample matches.
/// </summary>
public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<LeagueDbContext>();

        // Ensure database is created
        await context.Database.MigrateAsync();

        // Check if already seeded
        if (await context.Teams.AnyAsync())
        {
            Console.WriteLine("Database already seeded. Skipping...");
            return;
        }

        Console.WriteLine("Seeding database...");

        // 1. Create Divisions
        var divisions = await SeedDivisions(context);

        // 2. Create Teams
        var teams = await SeedTeams(context, divisions);

        // 3. Create Players
        await SeedPlayers(context, teams);

        // 4. Create Sample Matches
        var matches = await SeedMatches(context, teams, divisions);

        // 5. Create Sample Match Events
        await SeedMatchEvents(context, matches);

        Console.WriteLine("Database seeded successfully!");
    }

    private static async Task<List<Division>> SeedDivisions(LeagueDbContext context)
    {
        var currentSeason = DateTime.Now.Year;

        var divisions = new List<Division>
        {
            new Division
            {
                Name = "Premier League U15 Boys",
                ShortCode = "U15B",
                Season = currentSeason,
                AgeGroup = "U15",
                Gender = Gender.Male,
                IsActive = true,
                StartDate = new DateTime(currentSeason, 1, 15),
                EndDate = new DateTime(currentSeason, 11, 30),
                Description = "Premier division for boys under 15"
            },
            new Division
            {
                Name = "Premier League U15 Girls",
                ShortCode = "U15G",
                Season = currentSeason,
                AgeGroup = "U15",
                Gender = Gender.Female,
                IsActive = true,
                StartDate = new DateTime(currentSeason, 1, 15),
                EndDate = new DateTime(currentSeason, 11, 30),
                Description = "Premier division for girls under 15"
            },
            new Division
            {
                Name = "Senior Men's League",
                ShortCode = "SEN-M",
                Season = currentSeason,
                AgeGroup = "Senior",
                Gender = Gender.Male,
                IsActive = true,
                StartDate = new DateTime(currentSeason, 2, 1),
                EndDate = new DateTime(currentSeason, 12, 15),
                Description = "Top tier senior men's football"
            }
        };

        context.Divisions.AddRange(divisions);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {divisions.Count} divisions");

        return divisions;
    }

    private static async Task<List<Team>> SeedTeams(LeagueDbContext context, List<Division> divisions)
    {
        var seniorDivision = divisions.First(d => d.ShortCode == "SEN-M");

        var teams = new List<Team>
        {
            new Team
            {
                Name = "Orlando Pirates",
                ShortCode = "ORL",
                Founded = 1937,
                City = "Johannesburg",
                HomeGround = "Orlando Stadium",
                PrimaryColour = "#000000",
                SecondaryColour = "#FFFFFF",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/f/f3/Orlando_Pirates_logo.svg/200px-Orlando_Pirates_logo.svg.png",
                DivisionId = seniorDivision.Id
            },
            new Team
            {
                Name = "Kaizer Chiefs",
                ShortCode = "CHI",
                Founded = 1970,
                City = "Johannesburg",
                HomeGround = "FNB Stadium",
                PrimaryColour = "#FFD700",
                SecondaryColour = "#000000",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/b/bb/Kaizer_Chiefs_Logo.svg/200px-Kaizer_Chiefs_Logo.svg.png",
                DivisionId = seniorDivision.Id
            },
            new Team
            {
                Name = "Mamelodi Sundowns",
                ShortCode = "SUN",
                Founded = 1970,
                City = "Pretoria",
                HomeGround = "Loftus Versfeld",
                PrimaryColour = "#FFFF00",
                SecondaryColour = "#0000FF",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/f/f7/Mamelodi_Sundowns_F.C._logo.svg/200px-Mamelodi_Sundowns_F.C._logo.svg.png",
                DivisionId = seniorDivision.Id
            },
            new Team
            {
                Name = "SuperSport United",
                ShortCode = "SSU",
                Founded = 1994,
                City = "Pretoria",
                HomeGround = "Lucas Moripe Stadium",
                PrimaryColour = "#0000FF",
                SecondaryColour = "#FFFFFF",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/3/30/SuperSport_United_F.C._logo.svg/200px-SuperSport_United_F.C._logo.svg.png",
                DivisionId = seniorDivision.Id
            },
            new Team
            {
                Name = "AmaZulu FC",
                ShortCode = "AMA",
                Founded = 1932,
                City = "Durban",
                HomeGround = "Moses Mabhida Stadium",
                PrimaryColour = "#008000",
                SecondaryColour = "#FFFFFF",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/b/be/AmaZulu_FC_logo.svg/200px-AmaZulu_FC_logo.svg.png",
                DivisionId = seniorDivision.Id
            },
            new Team
            {
                Name = "Cape Town City",
                ShortCode = "CTC",
                Founded = 2016,
                City = "Cape Town",
                HomeGround = "Cape Town Stadium",
                PrimaryColour = "#00BFFF",
                SecondaryColour = "#FFFFFF",
                LogoUrl = "https://upload.wikimedia.org/wikipedia/en/thumb/9/90/Cape_Town_City_FC_logo.svg/200px-Cape_Town_City_FC_logo.svg.png",
                DivisionId = seniorDivision.Id
            }
        };

        context.Teams.AddRange(teams);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {teams.Count} teams");

        return teams;
    }

    private static async Task SeedPlayers(LeagueDbContext context, List<Team> teams)
    {
        var players = new List<Player>();
        var random = new Random();

        // Sample South African names
        var firstNames = new[] { "Thabo", "Sifiso", "Khanya", "Lebo", "Bongani", "Sipho", "Andile", "Thulani", "Mandla", "Jabu" };
        var lastNames = new[] { "Mkhize", "Dlamini", "Nkosi", "Khumalo", "Ngcobo", "Mokoena", "Radebe", "Molefe", "Tshabalala", "Zuma" };

        foreach (var team in teams)
        {
            // Create 18 players per team (2 GK, 6 DEF, 6 MID, 4 FWD)
            var positions = new[]
            {
                PlayerPosition.GK, PlayerPosition.GK,
                PlayerPosition.CB, PlayerPosition.CB, PlayerPosition.CB,
                PlayerPosition.CB, PlayerPosition.CB, PlayerPosition.CB,
                PlayerPosition.CM, PlayerPosition.CM, PlayerPosition.CM,
                PlayerPosition.CM, PlayerPosition.CM, PlayerPosition.CM,
                PlayerPosition.ST, PlayerPosition.ST, PlayerPosition.ST, PlayerPosition.ST
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var firstName = firstNames[random.Next(firstNames.Length)];
                var lastName = lastNames[random.Next(lastNames.Length)];
                var jerseyNumber = i + 1;

                // Generate placeholder avatar URL
                var avatarUrl = $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(firstName)}+{Uri.EscapeDataString(lastName)}&size=200&background=random&color=fff&bold=true";

                players.Add(new Player
                {
                    FirstName = firstName,
                    LastName = lastName,
                    ProfileImageUrl = avatarUrl,
                    DateOfBirth = DateTime.Now.AddYears(-random.Next(18, 35)),
                    Nationality = "South African",
                    JerseyNumber = jerseyNumber,
                    Position = positions[i],
                    IsActive = true,
                    Goals = positions[i] == PlayerPosition.ST ? random.Next(0, 15) : random.Next(0, 5),
                    Assists = positions[i] == PlayerPosition.CM ? random.Next(0, 10) : random.Next(0, 3),
                    YellowCards = random.Next(0, 5),
                    RedCards = random.Next(0, 2),
                    TeamId = team.Id
                });
            }
        }

        context.Players.AddRange(players);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {players.Count} players");
    }

    private static async Task<List<MatchResult>> SeedMatches(LeagueDbContext context, List<Team> teams, List<Division> divisions)
    {
        var seniorDivision = divisions.First(d => d.ShortCode == "SEN-M");
        var matches = new List<MatchResult>();
        var currentSeason = DateTime.Now.Year;
        var random = new Random();

        // Create round-robin fixtures (each team plays every other team once)
        int matchweek = 1;
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                var homeTeam = teams[i];
                var awayTeam = teams[j];
                var matchDate = DateTime.Now.AddDays(-30 + (matchweek * 7)); // Stagger matches weekly

                var match = new MatchResult
                {
                    MatchDate = matchDate,
                    MatchweekNumber = matchweek,
                    Season = currentSeason,
                    HomeTeamId = homeTeam.Id,
                    AwayTeamId = awayTeam.Id,
                    Venue = homeTeam.HomeGround,
                    Status = matchDate < DateTime.Now ? MatchStatus.Completed : MatchStatus.Scheduled,
                    DivisionId = seniorDivision.Id
                };

                // For completed matches, add random scores
                if (match.Status == MatchStatus.Completed)
                {
                    match.HomeScore = random.Next(0, 4);
                    match.AwayScore = random.Next(0, 4);
                }

                matches.Add(match);
                matchweek++;
            }
        }

        // Add a few upcoming matches
        for (int i = 0; i < 3; i++)
        {
            var homeTeam = teams[random.Next(teams.Count)];
            var awayTeam = teams[random.Next(teams.Count)];

            if (homeTeam.Id != awayTeam.Id)
            {
                matches.Add(new MatchResult
                {
                    MatchDate = DateTime.Now.AddDays(7 + (i * 3)),
                    MatchweekNumber = matchweek + i,
                    Season = currentSeason,
                    HomeTeamId = homeTeam.Id,
                    AwayTeamId = awayTeam.Id,
                    Venue = homeTeam.HomeGround,
                    Status = MatchStatus.Scheduled,
                    DivisionId = seniorDivision.Id
                });
            }
        }

        context.MatchResults.AddRange(matches);
        await context.SaveChangesAsync();
        Console.WriteLine($"✓ Created {matches.Count} matches");

        return matches;
    }

    private static async Task SeedMatchEvents(LeagueDbContext context, List<MatchResult> matches)
    {
        var events = new List<MatchEvent>();
        var random = new Random();

        // Add events only to completed matches
        var completedMatches = matches.Where(m => m.Status == MatchStatus.Completed).ToList();

        foreach (var match in completedMatches)
        {
            // Get players from both teams
            var homePlayers = await context.Players
                .Where(p => p.TeamId == match.HomeTeamId && p.IsActive)
                .ToListAsync();
            var awayPlayers = await context.Players
                .Where(p => p.TeamId == match.AwayTeamId && p.IsActive)
                .ToListAsync();

            // Add goal events for home team
            for (int i = 0; i < match.HomeScore; i++)
            {
                var scorer = homePlayers.Where(p => p.Position == PlayerPosition.ST || p.Position == PlayerPosition.CM)
                    .OrderBy(x => random.Next()).FirstOrDefault();

                if (scorer != null)
                {
                    events.Add(new MatchEvent
                    {
                        MatchId = match.Id,
                        PlayerId = scorer.Id,
                        EventType = EventType.Goal,
                        Minute = random.Next(1, 90),
                        AdditionalInfo = $"Goal scored by {scorer.FirstName} {scorer.LastName}"
                    });

                    // Sometimes add an assist
                    if (random.Next(0, 2) == 0)
                    {
                        var assister = homePlayers.Where(p => p.Id != scorer.Id && p.Position != PlayerPosition.GK)
                            .OrderBy(x => random.Next()).FirstOrDefault();
                        if (assister != null)
                        {
                            events.Add(new MatchEvent
                            {
                                MatchId = match.Id,
                                PlayerId = assister.Id,
                                EventType = EventType.Assist,
                                Minute = events.Last().Minute,
                                AdditionalInfo = $"Assisted by {assister.FirstName} {assister.LastName}"
                            });
                        }
                    }
                }
            }

            // Add goal events for away team
            for (int i = 0; i < match.AwayScore; i++)
            {
                var scorer = awayPlayers.Where(p => p.Position == PlayerPosition.ST || p.Position == PlayerPosition.CM)
                    .OrderBy(x => random.Next()).FirstOrDefault();

                if (scorer != null)
                {
                    events.Add(new MatchEvent
                    {
                        MatchId = match.Id,
                        PlayerId = scorer.Id,
                        EventType = EventType.Goal,
                        Minute = random.Next(1, 90),
                        AdditionalInfo = $"Goal scored by {scorer.FirstName} {scorer.LastName}"
                    });

                    // Sometimes add an assist
                    if (random.Next(0, 2) == 0)
                    {
                        var assister = awayPlayers.Where(p => p.Id != scorer.Id && p.Position != PlayerPosition.GK)
                            .OrderBy(x => random.Next()).FirstOrDefault();
                        if (assister != null)
                        {
                            events.Add(new MatchEvent
                            {
                                MatchId = match.Id,
                                PlayerId = assister.Id,
                                EventType = EventType.Assist,
                                Minute = events.Last().Minute,
                                AdditionalInfo = $"Assisted by {assister.FirstName} {assister.LastName}"
                            });
                        }
                    }
                }
            }

            // Add some yellow cards (randomly)
            var allPlayers = homePlayers.Concat(awayPlayers).ToList();
            var yellowCardCount = random.Next(0, 4);
            for (int i = 0; i < yellowCardCount; i++)
            {
                var player = allPlayers.OrderBy(x => random.Next()).FirstOrDefault();
                if (player != null)
                {
                    events.Add(new MatchEvent
                    {
                        MatchId = match.Id,
                        PlayerId = player.Id,
                        EventType = EventType.YellowCard,
                        Minute = random.Next(1, 90),
                        AdditionalInfo = "Foul"
                    });
                }
            }

            // Occasionally add a red card
            if (random.Next(0, 10) == 0)
            {
                var player = allPlayers.OrderBy(x => random.Next()).FirstOrDefault();
                if (player != null)
                {
                    events.Add(new MatchEvent
                    {
                        MatchId = match.Id,
                        PlayerId = player.Id,
                        EventType = EventType.RedCard,
                        Minute = random.Next(1, 90),
                        AdditionalInfo = "Serious foul play"
                    });
                }
            }
        }

        if (events.Any())
        {
            context.MatchEvents.AddRange(events);
            await context.SaveChangesAsync();
            Console.WriteLine($"✓ Created {events.Count} match events");
        }
    }
}
