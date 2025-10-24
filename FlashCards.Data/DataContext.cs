using FlashCards.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FlashCards.Data;

public partial class ApplicationDataContext(DbContextOptions<ApplicationDataContext> options)
    : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
}

public class ApplicationDataContextFactory : IDesignTimeDbContextFactory<ApplicationDataContext>
{
    public ApplicationDataContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDataContext>();

        // Absoluter Pfad zur Solution-Root
        var solutionRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".."));
        var dbPath = Path.Combine(solutionRoot, "flashcards.db");

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        Console.WriteLine($"DB wird erstellt in: {dbPath}");

        return new ApplicationDataContext(optionsBuilder.Options);
    }
}
