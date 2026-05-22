using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace iDiski.Infrastructure.Persistence;

public class LeagueDbContextFactory : IDesignTimeDbContextFactory<LeagueDbContext>
{
    public LeagueDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LeagueDbContext>();
        
        // Use your local Postgres connection string here
        optionsBuilder.UseNpgsql("Host=localhost; Port=5432; Database=idiski_db; Username=postgres; Password=localSoccerLeague@01");

        return new LeagueDbContext(optionsBuilder.Options);
    }
}